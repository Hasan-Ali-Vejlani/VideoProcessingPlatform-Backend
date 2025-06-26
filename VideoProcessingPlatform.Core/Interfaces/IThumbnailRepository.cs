// VideoProcessingPlatform.Core/Interfaces/IThumbnailRepository.cs
using VideoProcessingPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for data access operations related to Thumbnail entities.
    public interface IThumbnailRepository
    {
        // Adds a new thumbnail record to the database.
        Task AddAsync(Thumbnail thumbnail);

        // Gets a thumbnail by its unique ID.
        Task<Thumbnail?> GetByIdAsync(Guid id);

        // Gets all thumbnails associated with a specific video.
        Task<IEnumerable<Thumbnail>> GetByVideoIdAsync(Guid videoId);

        // Gets the currently default thumbnail for a specific video.
        Task<Thumbnail?> GetDefaultThumbnailByVideoIdAsync(Guid videoId);

        // Updates an existing thumbnail record.
        Task UpdateAsync(Thumbnail thumbnail);

        // Updates the IsDefault status for thumbnails of a specific video.
        // Sets the specified thumbnail as default and all others for that video as non-default.
        Task SetDefaultThumbnailAsync(Guid videoId, Guid thumbnailId);

        Task<bool> ThumbnailsExistForVideoAsync(Guid videoId);
    }
}
