// VideoProcessingPlatform.Core/Interfaces/IEncodingProfileInterfaces.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for data access operations related to EncodingProfile entities.
    public interface IEncodingProfileRepository
    {
        // Adds a new encoding profile to the repository.
        Task<EncodingProfile> Add(EncodingProfile profile);

        // Retrieves an encoding profile by its unique ID.
        Task<EncodingProfile?> GetById(Guid id);

        // Retrieves an encoding profile by its unique name.
        Task<EncodingProfile?> GetByName(string profileName);

        // Updates an existing encoding profile.
        Task<bool> Update(EncodingProfile profile);

        // Soft deletes an encoding profile by setting IsActive to false.
        Task<bool> SoftDelete(Guid id);

        // Retrieves all encoding profiles, including inactive ones (for admin view).
        Task<IEnumerable<EncodingProfile>> GetAll();

        // Retrieves all active encoding profiles (for user selection).
        Task<IEnumerable<EncodingProfile>> GetAllActive();
    }

    // Interface for building FFmpeg command arguments.
    // This abstracts the logic for constructing FFmpeg command strings from profile data.
    public interface IFFmpegCommandBuilder
    {
        // Builds the complete FFmpeg command arguments template for a given profile.
        // inputPathPlaceholder and outputPathPlaceholder are strings like "{inputPath}" that
        // will be replaced by actual paths at transcoding time.
        string BuildCommand(string resolution, int bitrateKbps, string format, string baseArgsTemplate,
                            string inputPathPlaceholder = "{inputPath}", string outputPathPlaceholder = "{outputPath}");

        // Validates if the provided FFmpegArgsTemplate contains the necessary placeholders
        // and basic structure. This can be a simple check or more complex parsing.
        bool ValidateTemplate(string template);
    }

    // Interface for managing encoding profiles business logic.
    public interface IEncodingProfileService
    {
        // Creates a new encoding profile.
        Task<EncodingProfileDto> CreateEncodingProfile(CreateEncodingProfileDto request);

        // Updates an existing encoding profile.
        Task<EncodingProfileDto> UpdateEncodingProfile(Guid id, UpdateEncodingProfileDto request);

        // Soft deletes an encoding profile (marks as inactive).
        Task<bool> SoftDeleteEncodingProfile(Guid id);

        // Retrieves a single encoding profile by ID.
        Task<EncodingProfileDto?> GetEncodingProfileById(Guid id);

        // Retrieves all encoding profiles (for admin).
        Task<IEnumerable<EncodingProfileDto>> GetAllEncodingProfiles();

        // Retrieves all active encoding profiles (for general user selection).
        Task<IEnumerable<EncodingProfileDto>> GetAllActiveEncodingProfiles();
    }
}
