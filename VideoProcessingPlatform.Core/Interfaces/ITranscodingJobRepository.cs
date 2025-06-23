// VideoProcessingPlatform.Core/Interfaces/ITranscodingJobRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.Entities;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for data access operations related to TranscodingJob and VideoRendition entities.
    public interface ITranscodingJobRepository
    {
        // Adds a new transcoding job.
        Task<TranscodingJob> Add(TranscodingJob job);

        // Updates an existing transcoding job.
        Task<bool> Update(TranscodingJob job);

        // Retrieves a transcoding job by its ID. Includes related entities (UploadMetadata, EncodingProfile, Renditions).
        Task<TranscodingJob?> GetById(Guid id);

        // Retrieves all transcoding jobs for a specific user.
        Task<IEnumerable<TranscodingJob>> GetByUserId(Guid userId);

        // Retrieves all transcoding jobs for a specific video (UploadMetadata).
        Task<IEnumerable<TranscodingJob>> GetByUploadMetadataId(Guid uploadMetadataId);

        // Adds a new video rendition to a transcoding job.
        Task<VideoRendition> AddRendition(VideoRendition rendition);

        // Updates an existing video rendition.
        Task<bool> UpdateRendition(VideoRendition rendition);

        // Retrieves all renditions for a specific transcoding job.
        Task<IEnumerable<VideoRendition>> GetRenditionsByJobId(Guid jobId);

        /// <summary>
        /// Retrieves completed video renditions for a specific video ID (UploadMetadataId).
        /// This method is crucial for the video playback feature to find available renditions.
        /// </summary>
        /// <param name="videoId">The ID of the original uploaded video (UploadMetadataId).</param>
        /// <returns>A collection of VideoRendition entities for completed renditions associated with the video.</returns>
        Task<IEnumerable<VideoRendition>> GetCompletedRenditionsForVideo(Guid videoId);
    }
}
