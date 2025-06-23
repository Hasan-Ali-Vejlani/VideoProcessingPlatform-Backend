// VideoProcessingPlatform.Infrastructure/Services/AzureBlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using VideoProcessingPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Implementation of IFileStorageService for Azure Blob Storage.
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _uploadChunksContainerName = "upload-chunks"; // Container for temporary chunks
        private readonly string _finalVideosContainerName = "final-videos"; // Container for merged videos
        private readonly string _thumbnailsContainerName = "thumbnails"; // Container for thumbnails
        private readonly string _renditionsContainerName = "renditions"; // Container for video renditions

        public AzureBlobStorageService(IConfiguration configuration)
        {
            // Retrieve the connection string from appsettings.json
            var connectionString = configuration.GetConnectionString("AzureBlobStorageConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("AzureBlobStorageConnection connection string is not configured.");
            }
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        // Helper method to get or create a blob container.
        private async Task<BlobContainerClient> GetOrCreateContainer(string containerName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            // --- FIX: Remove PublicAccessType.BlobContainer to align with Storage Account settings ---
            // If the storage account itself disallows public access, trying to set it at container level fails.
            // We want containers to be private anyway for security, relying on SAS/CDN.
            await containerClient.CreateIfNotExistsAsync(); // This will create as private if account disallows public.
            return containerClient;
        }

        // Stores a single chunk of a file in the 'upload-chunks' container.
        public async Task<string> StoreChunk(Guid uploadId, int chunkIndex, Stream chunkData)
        {
            var containerClient = await GetOrCreateContainer(_uploadChunksContainerName);
            string blobName = $"{uploadId}/{chunkIndex}";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            chunkData.Position = 0;
            await blobClient.UploadAsync(chunkData, overwrite: true);

            return blobClient.Uri.ToString();
        }

        // Merges all chunks for a given uploadId into a single final blob.
        public async Task<string> MergeChunks(Guid uploadId, string originalFileName, int totalChunks)
        {
            var chunksContainerClient = await GetOrCreateContainer(_uploadChunksContainerName);
            var finalVideosContainerClient = await GetOrCreateContainer(_finalVideosContainerName);

            string fileExtension = Path.GetExtension(originalFileName);
            string finalBlobName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_{uploadId}{fileExtension}";
            BlobClient finalBlobClient = finalVideosContainerClient.GetBlobClient(finalBlobName);

            using (var mergedStream = new MemoryStream())
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    string chunkBlobName = $"{uploadId}/{i}";
                    BlobClient chunkBlobClient = chunksContainerClient.GetBlobClient(chunkBlobName);

                    if (!await chunkBlobClient.ExistsAsync())
                    {
                        throw new FileNotFoundException($"Chunk {i} for upload {uploadId} not found during merge.");
                    }

                    var downloadResponse = await chunkBlobClient.DownloadContentAsync();
                    await downloadResponse.Value.Content.ToStream().CopyToAsync(mergedStream);

                    await chunkBlobClient.DeleteIfExistsAsync();
                }

                mergedStream.Position = 0;
                await finalBlobClient.UploadAsync(mergedStream, new BlobHttpHeaders { ContentType = GetContentType(fileExtension) });
            }

            await foreach (var blobItem in chunksContainerClient.GetBlobsAsync(prefix: $"{uploadId}/"))
            {
                await chunksContainerClient.GetBlobClient(blobItem.Name).DeleteIfExistsAsync();
            }

            return finalBlobClient.Uri.ToString();
        }

        // Retrieves a file stream from the storage service.
        public async Task<Stream> RetrieveFile(string path)
        {
            Uri uri = new Uri(path);
            string containerName = uri.Segments[1].TrimEnd('/');
            string blobName = string.Join("", uri.Segments.Skip(2)).TrimStart('/');

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File not found at: {path}");
            }

            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToStream();
        }

        // Stores a generated thumbnail image.
        public async Task<string> StoreThumbnail(Guid videoId, byte[] imageData, int index)
        {
            var containerClient = await GetOrCreateContainer(_thumbnailsContainerName);
            string blobName = $"{videoId}/{index}.jpg";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = new MemoryStream(imageData))
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = GetContentType(".jpg") });
            }

            return blobClient.Uri.ToString();
        }

        // Stores multiple video renditions.
        public async Task<List<string>> StoreRenditions(Guid jobId, Dictionary<string, Stream> renditions)
        {
            var containerClient = await GetOrCreateContainer(_renditionsContainerName);
            var storedPaths = new List<string>();

            foreach (var entry in renditions)
            {
                string renditionType = entry.Key;
                Stream renditionStream = entry.Value;

                string format = renditionType.Split('_').LastOrDefault()?.ToLower() ?? "mp4";

                string fileExtension = GetFileExtensionForFormat(format);
                string contentType = GetContentType(fileExtension);

                string blobName = $"{jobId}/{renditionType}{fileExtension}";

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                renditionStream.Position = 0;
                await blobClient.UploadAsync(renditionStream, new BlobHttpHeaders { ContentType = contentType });
                storedPaths.Add(blobClient.Uri.ToString());
            }

            return storedPaths;
        }

        private string GetFileExtensionForFormat(string format)
        {
            return format.ToLower() switch
            {
                "mp4" => ".mp4",
                "hls" => ".m3u8",
                "dash" => ".mpd",
                "webm" => ".webm",
                "ogg" => ".ogg",
                _ => ".bin",
            };
        }

        private string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".mp4" => "video/mp4",
                ".m3u8" => "application/x-mpegURL",
                ".ts" => "video/mp2t",
                ".mpd" => "application/dash+xml",
                ".webm" => "video/webm",
                ".ogg" => "video/ogg",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream",
            };
        }
    }
}
