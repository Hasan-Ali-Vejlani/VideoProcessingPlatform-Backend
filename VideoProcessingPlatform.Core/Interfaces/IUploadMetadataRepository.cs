// VideoProcessingPlatform.Core/Interfaces/IUploadMetadataRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.Entities;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for data access operations related to UploadMetadata entities.
    public interface IUploadMetadataRepository
    {
        // Adds new upload metadata to the repository.
        Task<UploadMetadata> Add(UploadMetadata uploadMetadata);

        // Retrieves upload metadata by its unique ID.
        Task<UploadMetadata?> GetById(Guid id);

        // Updates an existing upload metadata record.
        Task<bool> Update(UploadMetadata uploadMetadata);

        // Gets all upload metadata records for a specific user.
        Task<IEnumerable<UploadMetadata>> GetByUserId(Guid userId);

        // Gets upload metadata records for a specific user filtered by status.
        Task<IEnumerable<UploadMetadata>> GetByUserIdAndStatus(Guid userId, string status);
    }
}
