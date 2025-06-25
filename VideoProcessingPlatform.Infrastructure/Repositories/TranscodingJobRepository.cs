// VideoProcessingPlatform.Infrastructure/Repositories/TranscodingJobRepository.cs
using Microsoft.EntityFrameworkCore;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using VideoProcessingPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Infrastructure.Repositories
{
    // Concrete implementation of ITranscodingJobRepository using Entity Framework Core.
    public class TranscodingJobRepository : ITranscodingJobRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public TranscodingJobRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Adds a new transcoding job to the database.
        public async Task<TranscodingJob> Add(TranscodingJob job)
        {
            await _dbContext.TranscodingJobs.AddAsync(job);
            await _dbContext.SaveChangesAsync();
            return job;
        }

        // Updates an existing transcoding job in the database.
        public async Task<bool> Update(TranscodingJob job)
        {
            // Attach the entity if it's not already tracked, then mark as modified
            _dbContext.Entry(job).State = EntityState.Modified;
            // If job.VideoRenditions is loaded and contains entities, they might also be marked.
            // If only job properties are intended to be updated, consider fetching the entity first,
            // updating its properties, and then saving changes. For now, this is assumed to be fine.
            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Retrieves a transcoding job by its ID, including related entities.
        // This method is already comprehensive and includes UploadMetadata, EncodingProfile, and VideoRenditions.
        public async Task<TranscodingJob?> GetById(Guid id)
        {
            return await _dbContext.TranscodingJobs
                                       .Include(tj => tj.UploadMetadata) // Eager load UploadMetadata
                                       .Include(tj => tj.EncodingProfile) // Eager load EncodingProfile
                                       .Include(tj => tj.VideoRenditions) // Eager load associated renditions
                                       .FirstOrDefaultAsync(tj => tj.Id == id);
        }

        // Retrieves all transcoding jobs for a specific user.
        public async Task<IEnumerable<TranscodingJob>> GetByUserId(Guid userId)
        {
            return await _dbContext.TranscodingJobs
                                       .Include(tj => tj.UploadMetadata)
                                       .Include(tj => tj.EncodingProfile)
                                       .Include(tj => tj.VideoRenditions)
                                       .Where(tj => tj.UserId == userId)
                                       .OrderByDescending(tj => tj.CreatedAt)
                                       .ToListAsync();
        }

        // Retrieves all transcoding jobs for a specific video (UploadMetadata).
        public async Task<IEnumerable<TranscodingJob>> GetByUploadMetadataId(Guid uploadMetadataId)
        {
            return await _dbContext.TranscodingJobs
                                       .Include(tj => tj.UploadMetadata)
                                       .Include(tj => tj.EncodingProfile)
                                       .Include(tj => tj.VideoRenditions)
                                       .Where(tj => tj.UploadMetadataId == uploadMetadataId)
                                       .OrderByDescending(tj => tj.CreatedAt)
                                       .ToListAsync();
        }

        // Adds a new video rendition to the database.
        public async Task<VideoRendition> AddRendition(VideoRendition rendition)
        {
            await _dbContext.VideoRenditions.AddAsync(rendition);
            await _dbContext.SaveChangesAsync();
            return rendition;
        }

        // Updates an existing video rendition.
        public async Task<bool> UpdateRendition(VideoRendition rendition)
        {
            _dbContext.Entry(rendition).State = EntityState.Modified;
            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Retrieves all renditions for a specific transcoding job.
        public async Task<IEnumerable<VideoRendition>> GetRenditionsByJobId(Guid jobId)
        {
            return await _dbContext.VideoRenditions
                                       .Where(vr => vr.TranscodingJobId == jobId)
                                       .ToListAsync();
        }

        /// <summary>
        /// Retrieves completed video renditions for a specific video ID (UploadMetadataId).
        /// This method is crucial for the video playback feature to find available renditions.
        /// </summary>
        /// <param name="videoId">The ID of the original uploaded video (UploadMetadataId).</param>
        /// <returns>A collection of VideoRendition entities for completed renditions associated with the video.</returns>
        public async Task<IEnumerable<VideoRendition>> GetCompletedRenditionsForVideo(Guid videoId)
        {
            return await _dbContext.VideoRenditions
                                   .Include(r => r.TranscodingJob) // Ensure TranscodingJob is loaded for filtering
                                   .Where(r => r.TranscodingJob.UploadMetadataId == videoId &&
                                               r.TranscodingJob.Status == "Completed")
                                   .ToListAsync();
        }

        /// <summary>
        /// Retrieves the most recent transcoding job for a specific video, including its renditions.
        /// </summary>
        /// <param name="videoId">The ID of the original uploaded video (UploadMetadataId).</param>
        /// <returns>The latest TranscodingJob entity, or null if none found.</returns>
        public async Task<TranscodingJob?> GetLatestJobForVideo(Guid videoId)
        {
            return await _dbContext.TranscodingJobs
                                 .Include(tj => tj.VideoRenditions) // Eager load renditions
                                 .Include(tj => tj.UploadMetadata) // Include UploadMetadata for DTO mapping
                                 .Include(tj => tj.EncodingProfile) // Include EncodingProfile for DTO mapping
                                 .Where(tj => tj.UploadMetadataId == videoId)
                                 .OrderByDescending(tj => tj.CreatedAt) // --- FIX: Removed redundant 'tj.' here ---
                                 .FirstOrDefaultAsync();
        }
    }
}
