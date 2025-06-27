// VideoProcessingPlatform.Core/Interfaces/ITranscodingJobRepository.cs
using VideoProcessingPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for data access operations related to TranscodingJob entities.
    public interface ITranscodingJobRepository
    {
        // Adds a new transcoding job to the database.
        Task<TranscodingJob> Add(TranscodingJob job);

        // Updates an existing transcoding job in the database.
        Task<bool> Update(TranscodingJob job);

        // Retrieves a transcoding job by its ID, including related entities for detail view.
        Task<TranscodingJob?> GetById(Guid id);

        // Retrieves all transcoding jobs for a specific user.
        Task<IEnumerable<TranscodingJob>> GetByUserId(Guid userId);

        // Retrieves all transcoding jobs for a specific video (UploadMetadata).
        Task<IEnumerable<TranscodingJob>> GetByUploadMetadataId(Guid uploadMetadataId);

        // Adds a new video rendition to the database.
        Task<VideoRendition> AddRendition(VideoRendition rendition);

        // Updates an existing video rendition.
        Task<bool> UpdateRendition(VideoRendition rendition);

        // Retrieves all renditions for a specific transcoding job.
        Task<IEnumerable<VideoRendition>> GetRenditionsByJobId(Guid jobId);

        // Retrieves completed video renditions for a specific video ID.
        Task<IEnumerable<VideoRendition>> GetCompletedRenditionsForVideo(Guid videoId);

        // Retrieves the most recent transcoding job for a specific video
        Task<TranscodingJob?> GetLatestJobForVideo(Guid videoId);
    }
}
