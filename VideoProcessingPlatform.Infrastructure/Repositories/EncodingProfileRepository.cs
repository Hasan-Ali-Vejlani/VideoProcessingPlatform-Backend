// VideoProcessingPlatform.Infrastructure/Repositories/EncodingProfileRepository.cs
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
    // Concrete implementation of IEncodingProfileRepository using Entity Framework Core.
    public class EncodingProfileRepository : IEncodingProfileRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EncodingProfileRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Adds a new encoding profile to the database.
        public async Task<EncodingProfile> Add(EncodingProfile profile)
        {
            await _dbContext.EncodingProfiles.AddAsync(profile);
            await _dbContext.SaveChangesAsync();
            return profile;
        }

        // Retrieves an encoding profile by its ID.
        public async Task<EncodingProfile?> GetById(Guid id)
        {
            return await _dbContext.EncodingProfiles.FirstOrDefaultAsync(p => p.Id == id);
        }

        // Retrieves an encoding profile by its name.
        public async Task<EncodingProfile?> GetByName(string profileName)
        {
            return await _dbContext.EncodingProfiles.FirstOrDefaultAsync(p => p.ProfileName == profileName);
        }

        // Updates an existing encoding profile in the database.
        public async Task<bool> Update(EncodingProfile profile)
        {
            _dbContext.EncodingProfiles.Update(profile);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Soft deletes an encoding profile by setting IsActive to false.
        public async Task<bool> SoftDelete(Guid id)
        {
            var profile = await _dbContext.EncodingProfiles.FirstOrDefaultAsync(p => p.Id == id);
            if (profile == null)
            {
                return false;
            }

            profile.IsActive = false; // Mark as inactive
            profile.LastModifiedAt = DateTime.UtcNow; // Update modified timestamp
            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Retrieves all encoding profiles, including inactive ones.
        public async Task<IEnumerable<EncodingProfile>> GetAll()
        {
            return await _dbContext.EncodingProfiles.OrderBy(p => p.ProfileName).ToListAsync();
        }

        // Retrieves all active encoding profiles.
        public async Task<IEnumerable<EncodingProfile>> GetAllActive()
        {
            return await _dbContext.EncodingProfiles
                                   .Where(p => p.IsActive)
                                   .OrderBy(p => p.ProfileName)
                                   .ToListAsync();
        }
    }
}
