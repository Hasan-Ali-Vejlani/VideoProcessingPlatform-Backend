// VideoProcessingPlatform.Core/Interfaces/IUploadMetadataRepository.cs
using VideoProcessingPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for data access operations related to UploadMetadata entities.
    public interface IUploadMetadataRepository
    {
        // Adds a new UploadMetadata record to the database.
        Task<UploadMetadata> Add(UploadMetadata uploadMetadata);

        // Retrieves an UploadMetadata record by its ID.
        Task<UploadMetadata?> GetById(Guid id);

        // Updates an existing UploadMetadata record in the database.
        Task<bool> Update(UploadMetadata uploadMetadata);

        // Retrieves all UploadMetadata records for a given user ID.
        Task<IEnumerable<UploadMetadata>> GetByUserId(Guid userId);

        // Retrieves UploadMetadata records for a specific user filtered by status.
        Task<IEnumerable<UploadMetadata>> GetByUserIdAndStatus(Guid userId, string status);

        // Retrieves an UploadMetadata record by its ID, including all associated Thumbnail entities.
        Task<UploadMetadata?> GetByIdWithThumbnails(Guid id);
    }
}
