// VideoProcessingPlatform.Infrastructure/Services/ThumbnailService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.Extensions; // --- NEW: Add this using statement ---


namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Concrete implementation of IThumbnailService.
    public class ThumbnailService : IThumbnailService
    {
        private readonly IThumbnailRepository _thumbnailRepository;
        private readonly IUploadMetadataRepository _uploadMetadataRepository;
        private readonly ILogger<ThumbnailService> _logger;

        public ThumbnailService(
            IThumbnailRepository thumbnailRepository,
            IUploadMetadataRepository uploadMetadataRepository,
            ILogger<ThumbnailService> logger)
        {
            _thumbnailRepository = thumbnailRepository;
            _uploadMetadataRepository = uploadMetadataRepository;
            _logger = logger;
        }

        public async Task AddThumbnailMetadataAsync(Guid videoId, string storagePath, int timestampSeconds, int order, bool isDefault)
        {
            var thumbnail = new Thumbnail
            {
                UploadMetadataId = videoId,
                StoragePath = storagePath,
                TimestampSeconds = timestampSeconds,
                Order = order,
                IsDefault = isDefault,
                GeneratedAt = DateTime.UtcNow
            };
            await _thumbnailRepository.AddAsync(thumbnail);
            _logger.LogInformation($"Thumbnail metadata added for video {videoId}, order {order}. IsDefault: {isDefault}");

            if (isDefault)
            {
                var uploadMetadata = await _uploadMetadataRepository.GetById(videoId);
                if (uploadMetadata != null)
                {
                    uploadMetadata.SelectedThumbnailUrl = storagePath;
                    await _uploadMetadataRepository.Update(uploadMetadata);
                    _logger.LogInformation($"UploadMetadata for video {videoId} updated with selected thumbnail URL: {storagePath}");
                }
            }
        }

        public async Task<IEnumerable<ThumbnailDto>> GetThumbnailsForVideoAsync(Guid videoId)
        {
            var thumbnails = await _thumbnailRepository.GetByVideoIdAsync(videoId);
            return thumbnails.Select(t => t.ToDto()).ToList();
        }

        public async Task<ThumbnailDto?> GetDefaultThumbnailForVideoAsync(Guid videoId)
        {
            var thumbnail = await _thumbnailRepository.GetDefaultThumbnailByVideoIdAsync(videoId);
            return thumbnail?.ToDto();
        }

        public async Task<SetSelectedThumbnailResponseDto> SetDefaultThumbnailAsync(Guid videoId, Guid thumbnailId)
        {
            var response = new SetSelectedThumbnailResponseDto { Success = false };

            try
            {
                var uploadMetadata = await _uploadMetadataRepository.GetById(videoId);
                if (uploadMetadata == null)
                {
                    response.Message = $"Video with ID '{videoId}' not found.";
                    _logger.LogWarning(response.Message);
                    return response;
                }

                await _thumbnailRepository.SetDefaultThumbnailAsync(videoId, thumbnailId);

                var newDefaultThumbnail = await _thumbnailRepository.GetByIdAsync(thumbnailId);
                if (newDefaultThumbnail != null)
                {
                    uploadMetadata.SelectedThumbnailUrl = newDefaultThumbnail.StoragePath;
                    await _uploadMetadataRepository.Update(uploadMetadata);
                    response.NewSelectedThumbnailUrl = newDefaultThumbnail.StoragePath;
                    _logger.LogInformation($"Default thumbnail for video {videoId} set to {thumbnailId}. UploadMetadata.SelectedThumbnailUrl updated.");
                }
                else
                {
                    response.Message = $"New default thumbnail (ID: {thumbnailId}) not found after update operation.";
                    _logger.LogError(response.Message);
                    return response;
                }

                response.Success = true;
                response.Message = "Default thumbnail updated successfully.";
            }
            catch (KeyNotFoundException ex)
            {
                response.Message = ex.Message;
                _logger.LogError(ex, $"Failed to set default thumbnail for video {videoId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                response.Message = $"An unexpected error occurred while setting default thumbnail: {ex.Message}";
                _logger.LogError(ex, $"Failed to set default thumbnail for video {videoId}: {ex.Message}");
            }

            return response;
        }
    }
}
