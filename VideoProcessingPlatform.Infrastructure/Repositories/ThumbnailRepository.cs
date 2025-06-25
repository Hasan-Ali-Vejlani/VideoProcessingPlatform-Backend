// VideoProcessingPlatform.Infrastructure/Repositories/ThumbnailRepository.cs
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using VideoProcessingPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Infrastructure.Repositories
{
    // Concrete implementation of IThumbnailRepository using Entity Framework Core.
    public class ThumbnailRepository : IThumbnailRepository
    {
        private readonly ApplicationDbContext _context;

        public ThumbnailRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Thumbnail thumbnail)
        {
            if (thumbnail == null) throw new ArgumentNullException(nameof(thumbnail));
            await _context.Thumbnails.AddAsync(thumbnail);
            await _context.SaveChangesAsync();
        }

        public async Task<Thumbnail?> GetByIdAsync(Guid id)
        {
            return await _context.Thumbnails.FindAsync(id);
        }

        public async Task<IEnumerable<Thumbnail>> GetByVideoIdAsync(Guid videoId)
        {
            return await _context.Thumbnails
                                 .Where(t => t.UploadMetadataId == videoId)
                                 .OrderBy(t => t.Order) // Order them for consistent display
                                 .ToListAsync();
        }

        public async Task<Thumbnail?> GetDefaultThumbnailByVideoIdAsync(Guid videoId)
        {
            return await _context.Thumbnails
                                 .Where(t => t.UploadMetadataId == videoId && t.IsDefault)
                                 .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Thumbnail thumbnail)
        {
            if (thumbnail == null) throw new ArgumentNullException(nameof(thumbnail));
            _context.Thumbnails.Update(thumbnail);
            await _context.SaveChangesAsync();
        }

        public async Task SetDefaultThumbnailAsync(Guid videoId, Guid thumbnailId)
        {
            // Start a transaction to ensure atomicity
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Find the currently default thumbnail for this video and set IsDefault to false
                    var currentDefault = await _context.Thumbnails
                                                       .Where(t => t.UploadMetadataId == videoId && t.IsDefault)
                                                       .FirstOrDefaultAsync();
                    if (currentDefault != null)
                    {
                        currentDefault.IsDefault = false;
                        _context.Thumbnails.Update(currentDefault);
                    }

                    // 2. Find the new thumbnail and set IsDefault to true
                    var newDefault = await _context.Thumbnails
                                                   .Where(t => t.UploadMetadataId == videoId && t.Id == thumbnailId)
                                                   .FirstOrDefaultAsync();

                    if (newDefault == null)
                    {
                        throw new KeyNotFoundException($"Thumbnail with ID '{thumbnailId}' not found for video '{videoId}'.");
                    }

                    newDefault.IsDefault = true;
                    _context.Thumbnails.Update(newDefault);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw; // Re-throw the exception after rolling back
                }
            }
        }
    }
}
