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
            // Ensure related entities are not marked as modified unless explicitly intended
            // If you only want to update the job itself, you might need to load it first
            // and then update its properties from the detached 'job' object.
            // For now, assuming 'job' is retrieved and then passed back for update.

            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Retrieves a transcoding job by its ID, including related entities.
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
    }
}
