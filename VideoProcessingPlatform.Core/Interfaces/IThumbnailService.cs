// VideoProcessingPlatform.Core/Interfaces/IThumbnailService.cs
using VideoProcessingPlatform.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for business logic related to thumbnail management.
    public interface IThumbnailService
    {
        // Adds metadata for a new thumbnail after it's stored.
        Task AddThumbnailMetadataAsync(Guid videoId, string storagePath, int timestampSeconds, int order, bool isDefault);

        // Gets all thumbnails for a specific video, mapped to DTOs.
        Task<IEnumerable<ThumbnailDto>> GetThumbnailsForVideoAsync(Guid videoId);

        // Gets the default thumbnail for a video, mapped to a DTO.
        Task<ThumbnailDto?> GetDefaultThumbnailForVideoAsync(Guid videoId);

        // Sets a specific thumbnail as the default for a video, updating DB and UploadMetadata.
        Task<SetSelectedThumbnailResponseDto> SetDefaultThumbnailAsync(Guid videoId, Guid thumbnailId);
    }
}
