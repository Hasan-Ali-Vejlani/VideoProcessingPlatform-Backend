// VideoProcessingPlatform.Infrastructure/Services/AzureBlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas; // Required for BlobSasBuilder
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
        {
            var connectionString = configuration.GetConnectionString("AzureBlobStorageConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("AzureBlobStorageConnection connection string is not configured.");
            }
            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger = logger;

            EnsureContainersExistAsync().Wait();
        }

        private async Task EnsureContainersExistAsync()
        {
            _logger.LogInformation("Ensuring Azure Blob Storage containers exist...");
            await _blobServiceClient.GetBlobContainerClient(_uploadChunksContainerName).CreateIfNotExistsAsync();
            await _blobServiceClient.GetBlobContainerClient(_finalVideosContainerName).CreateIfNotExistsAsync();
            await _blobServiceClient.GetBlobContainerClient(_renditionsContainerName).CreateIfNotExistsAsync();
            await _blobServiceClient.GetBlobContainerClient(_thumbnailsContainerName).CreateIfNotExistsAsync();
            _logger.LogInformation("Azure Blob Storage containers checked/created.");
        }

        private async Task<BlobContainerClient> GetOrCreateContainer(string containerName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            return containerClient;
        }

        public async Task<string> StoreChunk(Guid uploadId, int chunkIndex, Stream chunkData)
        {
            var containerClient = await GetOrCreateContainer(_uploadChunksContainerName);
            string blobName = $"{uploadId}/{chunkIndex}";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (chunkData.CanSeek)
            {
                chunkData.Position = 0;
            }

            _logger.LogInformation($"Uploading chunk {chunkIndex} for upload {uploadId} to blob {blobName}. Stream length: {chunkData.Length}");

            try
            {
                await blobClient.UploadAsync(chunkData, overwrite: true);
                _logger.LogInformation($"Successfully uploaded chunk {chunkIndex} for upload {uploadId}.");
                return blobClient.Uri.ToString(); // This returns the full URI
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload chunk {chunkIndex} for upload {uploadId}.");
                throw;
            }
        }

        public async Task<string> MergeChunks(Guid uploadId, string originalFileName, int totalChunks)
        {
            var chunksContainerClient = await GetOrCreateContainer(_uploadChunksContainerName);
            var finalVideosContainerClient = await GetOrCreateContainer(_finalVideosContainerName);

            string fileExtension = Path.GetExtension(originalFileName);
            string finalBlobName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_{uploadId}{fileExtension}";
            BlobClient finalBlobClient = finalVideosContainerClient.GetBlobClient(finalBlobName);

            _logger.LogInformation($"Attempting to merge {totalChunks} chunks for upload {uploadId} into {finalBlobName}");

            using (var mergedStream = new MemoryStream())
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    string chunkBlobName = $"{uploadId}/{i}";
                    BlobClient chunkBlobClient = chunksContainerClient.GetBlobClient(chunkBlobName);

                    if (!await chunkBlobClient.ExistsAsync())
                    {
                        _logger.LogError($"Chunk {i} for upload {uploadId} not found during merge. Cannot complete merge.");
                        throw new FileNotFoundException($"Chunk {i} for upload {uploadId} not found during merge.");
                    }

                    try
                    {
                        var downloadResponse = await chunkBlobClient.DownloadContentAsync();
                        await downloadResponse.Value.Content.ToStream().CopyToAsync(mergedStream);
                        _logger.LogDebug($"Copied chunk {i} for upload {uploadId}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error downloading chunk {i} for upload {uploadId}.");
                        throw;
                    }
                    finally
                    {
                        await chunkBlobClient.DeleteIfExistsAsync();
                    }
                }

                mergedStream.Position = 0;
                await finalBlobClient.UploadAsync(mergedStream, new BlobHttpHeaders { ContentType = GetContentType(fileExtension) });
                _logger.LogInformation($"Successfully merged chunks and uploaded final video to {finalBlobClient.Uri}.");
            }

            await foreach (var blobItem in chunksContainerClient.GetBlobsAsync(prefix: $"{uploadId}/"))
            {
                try
                {
                    await chunksContainerClient.GetBlobClient(blobItem.Name).DeleteIfExistsAsync();
                    _logger.LogDebug($"Deleted leftover chunk blob: {blobItem.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete leftover chunk blob {blobItem.Name}: {ex.Message}");
                }
            }

            return finalBlobClient.Uri.ToString();
        }

        public async Task<Stream> RetrieveFile(string path)
        {
            // This method needs to handle both full URIs and relative paths if it's used for fetching from renditions.
            // For now, it expects a full URI, so the below parsing logic is fine.
            Uri uri;
            if (!Uri.TryCreate(path, UriKind.Absolute, out uri))
            {
                // If it's not an absolute URI, assume it's a relative path within a known container
                // This is a fallback and might need more robust container inference or explicit input.
                // For now, let's assume 'path' is a full URI if we reach here.
                _logger.LogError($"RetrieveFile received an invalid or relative path: {path}. Expected absolute URI.");
                throw new ArgumentException($"Invalid or relative path provided to RetrieveFile: {path}. Expected absolute URI.", nameof(path));
            }

            string containerName = uri.Segments[1].TrimEnd('/');
            string blobName = string.Join("", uri.Segments.Skip(2)).TrimStart('/');

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning($"File not found in blob storage at: {path}");
                throw new FileNotFoundException($"File not found at: {path}");
            }

            _logger.LogInformation($"Retrieving file from blob: {path}");
            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToStream();
        }

        public async Task<string> StoreThumbnail(Guid videoId, byte[] imageData, int index)
        {
            _logger.LogInformation($"Storing single thumbnail for video {videoId}, index {index}.");
            var containerClient = await GetOrCreateContainer(_thumbnailsContainerName);
            string blobName = $"{videoId}/{index}.jpg";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = new MemoryStream(imageData))
            {
                stream.Position = 0;
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = GetContentType(".jpg") });
            }

            _logger.LogInformation($"Stored single thumbnail: {blobClient.Uri}");
            return blobClient.Uri.ToString(); // This returns the full URI
        }

        public async Task<List<string>> StoreRenditions(Guid jobId, Dictionary<string, Stream> renditions)
        {
            var containerClient = await GetOrCreateContainer(_renditionsContainerName);
            var storedPaths = new List<string>();

            _logger.LogInformation($"Storing {renditions.Count} renditions for job {jobId}.");

            foreach (var entry in renditions)
            {
                string renditionType = entry.Key;
                Stream renditionStream = entry.Value;

                string format = renditionType.Split('_').LastOrDefault()?.ToLowerInvariant() ?? "mp4";

                string fileExtension = GetFileExtensionForFormat(format);
                string contentType = GetContentType(fileExtension);

                // Construct blob name as relative path within the container
                string blobName = $"{jobId}/{renditionType}{fileExtension}";

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                renditionStream.Position = 0;
                await blobClient.UploadAsync(renditionStream, new BlobHttpHeaders { ContentType = contentType });
                // --- FIX: Store relative path only, not full URI, if that's what's in the DB ---
                // Assuming the database column `VideoRendition.StoragePath` is designed to store
                // only the relative path (e.g., "renditions/jobId/file.mp4") and not the full blob URI.
                // If your DB expects full URI, change this back to blobClient.Uri.ToString();
                // storedPaths.Add($"{containerClient.Name}/{blobName}"); // Store "containerName/blobName"
                storedPaths.Add(blobClient.Uri.ToString());
                _logger.LogInformation($"Stored rendition {renditionType}. Relative Path: {containerClient.Name}/{blobName}");
            }

            return storedPaths;
        }

        /// <summary>
        /// Generates a Shared Access Signature (SAS) URL for a specific blob.
        /// This method now expects a 'blobRelativePath' in the format "containerName/blobName".
        /// </summary>
        /// <param name="blobRelativePath">The relative path of the blob (e.g., "renditions/jobId/file.mp4").</param>
        /// <param name="expiresIn">The duration for which the SAS URL will be valid.</param>
        /// <returns>The full Blob SAS URL.</returns>
        public async Task<string> GenerateBlobSasUrl(string blobRelativePath, TimeSpan expiresIn)
        {
            if (string.IsNullOrWhiteSpace(blobRelativePath))
            {
                throw new ArgumentNullException(nameof(blobRelativePath), "Blob relative path cannot be null or empty.");
            }
            if (expiresIn <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expiresIn), "Expiration time must be positive.");
            }

            try
            {
                // Split the blobRelativePath into container and blob name
                // Expects format: "containerName/blobName"
                string[] segments = blobRelativePath.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 2)
                {
                    _logger.LogError($"Invalid blob relative path format provided for SAS generation: '{blobRelativePath}'. Expected 'containerName/blobName'.");
                    throw new ArgumentException($"Invalid blob relative path format. Expected 'containerName/blobName'.", nameof(blobRelativePath));
                }

                string containerName = segments[0];
                string blobName = segments[1];

                // Get the BlobClient using the identified container and blob name
                BlobClient blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning($"Attempted to generate SAS for non-existent blob (constructed path): {containerName}/{blobName}");
                    throw new FileNotFoundException($"Blob not found: {containerName}/{blobName}");
                }

                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b", // "b" for blob
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Start a few minutes in the past to account for clock skew
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                _logger.LogInformation($"Generated SAS URL for {containerName}/{blobName}. Expires on: {sasBuilder.ExpiresOn}");
                return sasUri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating SAS URL for blob relative path: {blobRelativePath}.");
                throw;
            }
        }

        public async Task DeleteFile(string filePath)
        {
            Uri uri;
            BlobClient blobClient;

            // Attempt to parse as absolute URI first (e.g., if full URI was stored)
            if (Uri.TryCreate(filePath, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                string containerName = uri.Segments[1].TrimEnd('/');
                string blobName = string.Join("", uri.Segments.Skip(2)).TrimStart('/');
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                blobClient = containerClient.GetBlobClient(blobName);
            }
            else
            {
                // Assume it's a relative path like "containerName/blobName"
                string[] segments = filePath.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2)
                {
                    _logger.LogWarning($"DeleteFile received an invalid path format: {filePath}. Expected absolute URI or 'container/blobname'. Skipping delete.");
                    return; // Cannot determine blob to delete
                }
                string containerName = segments[0];
                string blobName = segments[1];
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                blobClient = containerClient.GetBlobClient(blobName);
            }

            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                _logger.LogInformation($"Deleted blob: {filePath}");
            }
            else
            {
                _logger.LogWarning($"Attempted to delete non-existent blob: {filePath}");
            }
        }

        private string GetFileExtensionForFormat(string format)
        {
            return format.ToLowerInvariant() switch
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
            return fileExtension.ToLowerInvariant() switch
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
