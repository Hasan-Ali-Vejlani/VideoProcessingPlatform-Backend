//// VideoProcessingPlatform.Api/Services/VideoPlaybackService.cs
//using VideoProcessingPlatform.Core.DTOs;
//using VideoProcessingPlatform.Core.Entities; // Required to use VideoRendition entity
//using VideoProcessingPlatform.Core.Interfaces;
//using Microsoft.Extensions.Logging; // Required for ILogger
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic; // Required for List

//namespace VideoProcessingPlatform.Api.Services
//{
//    public class VideoPlaybackService : IVideoPlaybackService
//    {
//        private readonly ITranscodingJobRepository _transcodingJobRepository;
//        private readonly ICDNService _cdnService;
//        private readonly ILogger<VideoPlaybackService> _logger; // Add logger

//        public VideoPlaybackService(ITranscodingJobRepository transcodingJobRepository, ICDNService cdnService, ILogger<VideoPlaybackService> logger)
//        {
//            _transcodingJobRepository = transcodingJobRepository;
//            _cdnService = cdnService;
//            _logger = logger; // Initialize logger
//        }

//        public async Task<SignedUrlDto> GetSignedVideoUrl(Guid videoId, string requestedRenditionType, Guid userId)
//        {
//            _logger.LogInformation($"Attempting to get signed URL for VideoId: {videoId}, Requested Rendition: {requestedRenditionType}, User: {userId}");

//            // 1. Retrieve completed renditions (entities) for the video
//            // videoId here refers to UploadMetadataId, so we fetch renditions associated with jobs linked to this UploadMetadata.
//            var renditionEntities = await _transcodingJobRepository.GetCompletedRenditionsForVideo(videoId);

//            if (renditionEntities == null || !renditionEntities.Any())
//            {
//                _logger.LogWarning($"No completed renditions found for VideoId: {videoId}.");
//                return new SignedUrlDto { Success = false, Message = "No completed renditions found for this video." };
//            }

//            // Map entities to DTOs for consistent service-level handling and filtering/sorting.
//            var availableRenditions = renditionEntities.Select(r => new VideoRenditionDto
//            {
//                Id = r.Id,
//                RenditionType = r.RenditionType,
//                StoragePath = r.StoragePath,
//                IsEncrypted = r.IsEncrypted,
//                Resolution = r.Resolution,    // These are now populated from the updated VideoRendition entity
//                BitrateKbps = r.BitrateKbps,  // These are now populated from the updated VideoRendition entity
//                PlaybackUrl = null // Will be set after signed URL generation
//            }).ToList();


//            // 2. Find the requested rendition type
//            VideoRenditionDto? targetRendition = null;

//            // Prioritize exact match first
//            targetRendition = availableRenditions.FirstOrDefault(r =>
//                r.RenditionType.Equals(requestedRenditionType, StringComparison.OrdinalIgnoreCase)
//            );

//            // If a specific rendition type is requested and not found, try to find a suitable alternative.
//            if (targetRendition == null)
//            {
//                _logger.LogInformation($"Requested rendition type '{requestedRenditionType}' not found for video {videoId}. Attempting fallback.");

//                // Fallback logic:
//                // Option A: Try to find an MP4 version first (common browser compatibility)
//                targetRendition = availableRenditions
//                    .Where(r => r.RenditionType.EndsWith("_mp4", StringComparison.OrdinalIgnoreCase))
//                    .OrderByDescending(r => r.BitrateKbps) // Prefer higher quality MP4
//                    .FirstOrDefault();

//                // Option B: If no MP4, get the highest quality available rendition regardless of format
//                if (targetRendition == null)
//                {
//                    targetRendition = availableRenditions
//                        .OrderByDescending(r => r.BitrateKbps) // Order by bitrate for perceived quality
//                        .ThenByDescending(r => r.Resolution)    // Then by resolution
//                        .FirstOrDefault();
//                }

//                if (targetRendition == null)
//                {
//                    _logger.LogWarning($"No suitable rendition found for VideoId: {videoId} after fallback attempts.");
//                    return new SignedUrlDto { Success = false, Message = $"Rendition type '{requestedRenditionType}' not found and no other suitable renditions available for video {videoId}." };
//                }

//                _logger.LogInformation($"Falling back to rendition type: '{targetRendition.RenditionType}' for VideoId: {videoId}.");
//            }

//            // 3. Generate a signed URL for the storage path of the rendition
//            // The StoragePath from the entity is a full Azure Blob URL (e.g., https://<account>.blob.core.windows.net/renditions/jobId/file.mp4)
//            // The CDN service needs the path relative to the CDN origin.
//            // Assuming your CDN origin is linked to the storage account directly,
//            // the relative path will start from the container name.
//            string relativePathToCdnOrigin = GetRelativePathForCdn(targetRendition.StoragePath);

//            if (string.IsNullOrEmpty(relativePathToCdnOrigin))
//            {
//                _logger.LogError($"Could not determine relative path for CDN from storage path: {targetRendition.StoragePath}");
//                return new SignedUrlDto { Success = false, Message = "Failed to process storage path for CDN." };
//            }

//            // The expiresIn TimeSpan should be configurable (e.g., 1 hour from appsettings)
//            var signedUrl = await _cdnService.GenerateSignedUrl(relativePathToCdnOrigin, TimeSpan.FromHours(1));

//            if (string.IsNullOrEmpty(signedUrl))
//            {
//                _logger.LogError($"Failed to generate signed URL for storage path: {targetRendition.StoragePath}");
//                return new SignedUrlDto { Success = false, Message = "Failed to generate signed URL." };
//            }

//            _logger.LogInformation($"Successfully generated signed URL for VideoId: {videoId}, Rendition: {targetRendition.RenditionType}");

//            return new SignedUrlDto
//            {
//                Success = true,
//                Url = signedUrl,
//                Message = "Signed URL generated successfully."
//            };
//        }

//        /// <summary>
//        /// Extracts the relative path suitable for CDN from a full Azure Blob Storage URL.
//        /// Assumes the CDN origin is the root of the storage account or a specific container.
//        /// Example: "https://<account>.blob.core.windows.net/renditions/jobid/file.mp4"
//        /// should become "renditions/jobid/file.mp4"
//        /// </summary>
//        /// <param name="fullStoragePath">The full URL of the blob in Azure Storage.</param>
//        /// <returns>The relative path for the CDN, or null if the path cannot be parsed.</returns>
//        private string GetRelativePathForCdn(string fullStoragePath)
//        {
//            if (Uri.TryCreate(fullStoragePath, UriKind.Absolute, out Uri? uri))
//            {
//                // The AbsolutePath property gives you everything after the domain, including the leading slash.
//                // For example, for "https://account.blob.core.windows.net/container/path/file.mp4"
//                // AbsolutePath will be "/container/path/file.mp4"
//                // We trim the leading slash to get "container/path/file.mp4"
//                return uri.AbsolutePath.TrimStart('/');
//            }
//            _logger.LogError($"Invalid storage path URI: {fullStoragePath}");
//            return string.Empty; // Return empty string or throw, depending on desired error handling
//        }
//    }
//}

// VideoProcessingPlatform.Api/Services/VideoPlaybackService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities; // Required to use VideoRendition entity
using VideoProcessingPlatform.Core.Interfaces;
using Microsoft.Extensions.Logging; // Required for ILogger
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for List

namespace VideoProcessingPlatform.Api.Services
{
    public class VideoPlaybackService : IVideoPlaybackService
    {
        private readonly ITranscodingJobRepository _transcodingJobRepository;
        private readonly ICDNService _cdnService;
        private readonly ILogger<VideoPlaybackService> _logger;

        public VideoPlaybackService(ITranscodingJobRepository transcodingJobRepository, ICDNService cdnService, ILogger<VideoPlaybackService> logger)
        {
            _transcodingJobRepository = transcodingJobRepository;
            _cdnService = cdnService;
            _logger = logger;
        }

        public async Task<SignedUrlDto> GetSignedVideoUrl(Guid videoId, string requestedRenditionType, Guid userId)
        {
            _logger.LogInformation($"Attempting to get signed URL for VideoId: {videoId}, Requested Rendition: {requestedRenditionType}, User: {userId}");

            // 1. Retrieve completed renditions (entities) for the video
            var renditionEntities = await _transcodingJobRepository.GetCompletedRenditionsForVideo(videoId);

            if (renditionEntities == null || !renditionEntities.Any())
            {
                _logger.LogWarning($"No completed renditions found for VideoId: {videoId}.");
                return new SignedUrlDto { Success = false, Message = "No completed renditions found for this video." };
            }

            // Map entities to DTOs for consistent service-level handling and filtering/sorting.
            // Also, sort them by quality (bitrate then resolution) for a sensible default or display order.
            var availableRenditions = renditionEntities
                .Select(r => new VideoRenditionDto
                {
                    Id = r.Id,
                    RenditionType = r.RenditionType,
                    StoragePath = r.StoragePath,
                    IsEncrypted = r.IsEncrypted,
                    Resolution = r.Resolution,
                    BitrateKbps = r.BitrateKbps,
                    PlaybackUrl = null // Will be set after signed URL generation for the target rendition
                })
                .OrderByDescending(r => r.BitrateKbps)
                .ThenByDescending(r => r.Resolution)
                .ToList();


            // 2. Find the requested rendition type
            VideoRenditionDto? targetRendition = null;

            // Prioritize exact match first
            targetRendition = availableRenditions.FirstOrDefault(r =>
                r.RenditionType.Equals(requestedRenditionType, StringComparison.OrdinalIgnoreCase)
            );

            // Fallback logic: If a specific rendition type is requested and not found,
            // get the highest quality available rendition.
            if (targetRendition == null)
            {
                _logger.LogInformation($"Requested rendition type '{requestedRenditionType}' not found for video {videoId}. Falling back to highest quality available.");
                targetRendition = availableRenditions.FirstOrDefault(); // This will be the first (highest quality) due to prior OrderByDescending
            }

            if (targetRendition == null) // Should not happen if availableRenditions was not empty
            {
                _logger.LogWarning($"No suitable rendition found for VideoId: {videoId} after fallback attempts.");
                return new SignedUrlDto { Success = false, Message = $"No suitable renditions available for video {videoId}." };
            }

            _logger.LogInformation($"Using rendition type: '{targetRendition.RenditionType}' for VideoId: {videoId}.");

            // 3. Generate a signed URL for the storage path of the target rendition
            string relativePathToCdnOrigin = GetRelativePathForCdn(targetRendition.StoragePath);

            if (string.IsNullOrEmpty(relativePathToCdnOrigin))
            {
                _logger.LogError($"Could not determine relative path for CDN from storage path: {targetRendition.StoragePath}");
                return new SignedUrlDto { Success = false, Message = "Failed to process storage path for CDN." };
            }

            // The expiresIn TimeSpan should be configurable (e.g., 1 hour from appsettings)
            var signedUrl = await _cdnService.GenerateSignedUrl(relativePathToCdnOrigin, TimeSpan.FromHours(1));

            if (string.IsNullOrEmpty(signedUrl))
            {
                _logger.LogError($"Failed to generate signed URL for storage path: {targetRendition.StoragePath}");
                return new SignedUrlDto { Success = false, Message = "Failed to generate signed URL." };
            }

            _logger.LogInformation($"Successfully generated signed URL for VideoId: {videoId}, Rendition: {targetRendition.RenditionType}");

            return new SignedUrlDto
            {
                Success = true,
                Url = signedUrl,
                Message = "Signed URL generated successfully.",
                AvailableRenditions = availableRenditions // <--- NEW: Include the list of all available renditions
            };
        }

        /// <summary>
        /// Extracts the relative path suitable for CDN from a full Azure Blob Storage URL.
        /// Assumes the CDN origin is the root of the storage account or a specific container.
        /// Example: "https://<account>.blob.core.windows.net/renditions/jobid/file.mp4"
        /// should become "renditions/jobid/file.mp4"
        /// </summary>
        /// <param name="fullStoragePath">The full URL of the blob in Azure Storage.</param>
        /// <returns>The relative path for the CDN, or null if the path cannot be parsed.</returns>
        private string GetRelativePathForCdn(string fullStoragePath)
        {
            if (Uri.TryCreate(fullStoragePath, UriKind.Absolute, out Uri? uri))
            {
                // The AbsolutePath property gives you everything after the domain, including the leading slash.
                // For example, for "https://account.blob.core.windows.net/container/path/file.mp4"
                // AbsolutePath will be "/container/path/file.mp4"
                // We trim the leading slash to get "container/path/file.mp4"
                // This assumes CDN origin is mapped to the root of the storage account.
                // If CDN origin is mapped to a specific container (e.g., /renditions),
                // then you might need to adjust this to ensure the path is relative to that.
                // For now, it correctly gives "container/blobpath"
                return uri.AbsolutePath.TrimStart('/');
            }
            _logger.LogError($"Invalid storage path URI: {fullStoragePath}");
            return string.Empty; // Return empty string or throw, depending on desired error handling
        }
    }
}
