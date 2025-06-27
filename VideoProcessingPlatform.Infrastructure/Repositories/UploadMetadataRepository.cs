// VideoProcessingPlatform.Infrastructure/Repositories/UploadMetadataRepository.cs
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
    // Concrete implementation of IUploadMetadataRepository using Entity Framework Core.
    public class UploadMetadataRepository : IUploadMetadataRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UploadMetadataRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Adds a new UploadMetadata record to the database.
        public async Task<UploadMetadata> Add(UploadMetadata uploadMetadata)
        {
            await _dbContext.UploadMetadata.AddAsync(uploadMetadata);
            await _dbContext.SaveChangesAsync();
            return uploadMetadata;
        }

        // Retrieves an UploadMetadata record by its ID.
        public async Task<UploadMetadata?> GetById(Guid id)
        {
            return await _dbContext.UploadMetadata
                                   .FirstOrDefaultAsync(um => um.Id == id);
        }

        // Updates an existing UploadMetadata record in the database.
        public async Task<bool> Update(UploadMetadata uploadMetadata)
        {
            _dbContext.UploadMetadata.Update(uploadMetadata);
            // SaveChangesAsync returns the number of state entries written to the database.
            // If > 0, it means the update was successful.
            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Retrieves all UploadMetadata records for a given user ID.
        public async Task<IEnumerable<UploadMetadata>> GetByUserId(Guid userId)
        {
            return await _dbContext.UploadMetadata
                                   .Where(um => um.UserId == userId)
                                   .OrderByDescending(um => um.UploadedAt) // Order by latest uploads first
                                   .ToListAsync();
        }

        // Retrieves UploadMetadata records for a specific user filtered by status.
        public async Task<IEnumerable<UploadMetadata>> GetByUserIdAndStatus(Guid userId, string status)
        {
            return await _dbContext.UploadMetadata
                                   .Where(um => um.UserId == userId && um.UploadStatus == status)
                                   .OrderByDescending(um => um.UploadedAt)
                                   .ToListAsync();
        }

        /// <summary>
        /// Retrieves an UploadMetadata record by its ID, including all associated Thumbnail entities.
        /// </summary>
        /// <param name="id">The unique ID of the UploadMetadata record.</param>
        /// <returns>The UploadMetadata entity with Thumbnails eagerly loaded, or null if not found.</returns>
        public async Task<UploadMetadata?> GetByIdWithThumbnails(Guid id)
        {
            return await _dbContext.UploadMetadata
                                 .Include(um => um.Thumbnails) // Eager load thumbnails
                                 .FirstOrDefaultAsync(um => um.Id == id);
        }
    }
}
