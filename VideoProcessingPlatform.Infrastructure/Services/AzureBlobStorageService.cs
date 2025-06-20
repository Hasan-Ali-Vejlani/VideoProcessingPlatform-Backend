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
            await containerClient.CreateIfNotExistsAsync();
            return containerClient;
        }

        // Stores a single chunk of a file in the 'upload-chunks' container.
        public async Task<string> StoreChunk(Guid uploadId, int chunkIndex, Stream chunkData)
        {
            var containerClient = await GetOrCreateContainer(_uploadChunksContainerName);
            // Chunk filenames will be like: {uploadId}/{chunkIndex}
            string blobName = $"{uploadId}/{chunkIndex}";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            chunkData.Position = 0; // Ensure stream is at the beginning
            await blobClient.UploadAsync(chunkData, overwrite: true); // Overwrite if chunk is re-uploaded

            return blobClient.Uri.ToString(); // Return the URI of the stored chunk
        }

        // Merges all chunks for a given uploadId into a single final blob.
        public async Task<string> MergeChunks(Guid uploadId, string originalFileName, int totalChunks)
        {
            var chunksContainerClient = await GetOrCreateContainer(_uploadChunksContainerName);
            var finalVideosContainerClient = await GetOrCreateContainer(_finalVideosContainerName);

            // Construct the final blob name (e.g., originalFileName_uploadId.ext)
            string fileExtension = Path.GetExtension(originalFileName);
            string finalBlobName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_{uploadId}{fileExtension}";
            BlobClient finalBlobClient = finalVideosContainerClient.GetBlobClient(finalBlobName);

            // Create a list of block IDs. Azure Blob Storage supports putting blocks together.
            // For simplicity here, we'll download chunks and re-upload.
            // For very large files, consider using Azure Blob's Block Blob features (PutBlock, PutBlockList).
            // This implementation downloads each chunk and writes to a MemoryStream.
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

                    await chunkBlobClient.DownloadToAsync(mergedStream);
                    // Optionally, delete the chunk blob after downloading to save space
                    await chunkBlobClient.DeleteIfExistsAsync();
                }

                mergedStream.Position = 0; // Reset stream position to the beginning before uploading
                await finalBlobClient.UploadAsync(mergedStream, new BlobHttpHeaders { ContentType = "video/mp4" }); // Set appropriate content type
            }

            // After merging, delete the temporary chunk directory (or all blobs with prefix)
            // Listing and deleting blobs with the prefix "{uploadId}/"
            await foreach (var blobItem in chunksContainerClient.GetBlobsByHierarchyAsync(prefix: $"{uploadId}/"))
            {
                if (blobItem.IsBlob)
                {
                    await chunksContainerClient.GetBlobClient(blobItem.Prefix).DeleteIfExistsAsync();
                }
            }

            return finalBlobClient.Uri.ToString(); // Return the URI of the final merged file
        }

        // Retrieves a file stream from the storage service.
        public async Task<Stream> RetrieveFile(string path)
        {
            // Parse the path to determine container and blob name
            // Assuming 'path' is a full URI or a relative path indicating container/blob
            Uri uri = new Uri(path);
            string containerName = uri.Segments[1].TrimEnd('/'); // Get the container name from the URL
            string blobName = string.Join("", uri.Segments.Skip(2)).TrimStart('/'); // Get the blob name

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File not found at: {path}");
            }

            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToStream(); // Return the content as a stream
        }

        // Stores a generated thumbnail image.
        public async Task<string> StoreThumbnail(Guid videoId, byte[] imageData, int index)
        {
            var containerClient = await GetOrCreateContainer(_thumbnailsContainerName);
            // Thumbnail filenames: {videoId}/{index}.jpg
            string blobName = $"{videoId}/{index}.jpg";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = new MemoryStream(imageData))
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "image/jpeg" });
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
                string renditionType = entry.Key; // e.g., "HLS_720p"
                Stream renditionStream = entry.Value;

                // Rendition filenames: {jobId}/{renditionType}/manifest.m3u8 (or similar)
                // Assuming renditionType might include format and resolution, e.g., "HLS_720p"
                // For HLS, it's often a directory with a manifest and TS files.
                // For DASH, similar with an MPD file and segments.
                // This example stores a single file per rendition type.
                // In a real scenario, you'd handle HLS/DASH manifest and segment files more granularly.
                string blobName = $"{jobId}/{renditionType}/output"; // A generic name, actual file type will vary

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                renditionStream.Position = 0;
                // You might need to infer content type or pass it in
                await blobClient.UploadAsync(renditionStream, overwrite: true);
                storedPaths.Add(blobClient.Uri.ToString());
            }

            return storedPaths;
        }
    }
}
