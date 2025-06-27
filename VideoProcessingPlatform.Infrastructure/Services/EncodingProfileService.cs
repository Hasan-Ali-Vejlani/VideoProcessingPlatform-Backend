// VideoProcessingPlatform.Infrastructure/Services/EncodingProfileService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    public class EncodingProfileService : IEncodingProfileService
    {
        private readonly IEncodingProfileRepository _profileRepository;
        private readonly IFFmpegCommandBuilder _ffmpegCommandBuilder;

        public EncodingProfileService(IEncodingProfileRepository profileRepository, IFFmpegCommandBuilder ffmpegCommandBuilder)
        {
            _profileRepository = profileRepository;
            _ffmpegCommandBuilder = ffmpegCommandBuilder;
        }

        // Creates a new encoding profile.
        public async Task<EncodingProfileDto> CreateEncodingProfile(CreateEncodingProfileDto request)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(request.ProfileName))
            {
                throw new ArgumentException("Profile name cannot be empty.", nameof(request.ProfileName));
            }

            // 2. Check for duplicate profile name
            var existingProfile = await _profileRepository.GetByName(request.ProfileName);
            if (existingProfile != null)
            {
                throw new InvalidOperationException($"Encoding profile with name '{request.ProfileName}' already exists.");
            }

            // 3. Validate FFmpegArgsTemplate structure
            if (!_ffmpegCommandBuilder.ValidateTemplate(request.FFmpegArgsTemplate))
            {
                throw new ArgumentException("FFmpeg arguments template is invalid. It must contain {inputPath} and {outputPath} placeholders.", nameof(request.FFmpegArgsTemplate));
            }

            // 4. Build/finalize FFmpegArgsTemplate (though in this design, we take it directly from DTO)

            // 5. Create entity
            var profile = new EncodingProfile
            {
                ProfileName = request.ProfileName,
                Description = request.Description,
                Resolution = request.Resolution,
                BitrateKbps = request.BitrateKbps,
                Format = request.Format,
                FFmpegArgsTemplate = request.FFmpegArgsTemplate, // Using directly from request for now
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            // 6. Add to repository
            var createdProfile = await _profileRepository.Add(profile);

            // 7. Map to DTO and return
            return MapToDto(createdProfile);
        }

        // Updates an existing encoding profile.
        public async Task<EncodingProfileDto> UpdateEncodingProfile(Guid id, UpdateEncodingProfileDto request)
        {
            // 1. Retrieve existing profile
            var existingProfile = await _profileRepository.GetById(id);
            if (existingProfile == null)
            {
                throw new KeyNotFoundException($"Encoding profile with ID '{id}' not found.");
            }

            // 2. Validate input
            if (string.IsNullOrWhiteSpace(request.ProfileName))
            {
                throw new ArgumentException("Profile name cannot be empty.", nameof(request.ProfileName));
            }

            // 3. Check for duplicate profile name (if name is changed and conflicts with another existing profile)
            if (existingProfile.ProfileName != request.ProfileName)
            {
                var profileWithSameName = await _profileRepository.GetByName(request.ProfileName);
                if (profileWithSameName != null && profileWithSameName.Id != id)
                {
                    throw new InvalidOperationException($"Encoding profile with name '{request.ProfileName}' already exists for another profile.");
                }
            }

            // 4. Validate FFmpegArgsTemplate structure
            if (!_ffmpegCommandBuilder.ValidateTemplate(request.FFmpegArgsTemplate))
            {
                throw new ArgumentException("FFmpeg arguments template is invalid. It must contain {inputPath} and {outputPath} placeholders.", nameof(request.FFmpegArgsTemplate));
            }

            // 5. Update entity properties
            existingProfile.ProfileName = request.ProfileName;
            existingProfile.Description = request.Description;
            existingProfile.Resolution = request.Resolution;
            existingProfile.BitrateKbps = request.BitrateKbps;
            existingProfile.Format = request.Format;
            existingProfile.FFmpegArgsTemplate = request.FFmpegArgsTemplate; // Using directly from request for now
            existingProfile.IsActive = request.IsActive; // Admin can set active/inactive
            existingProfile.LastModifiedAt = DateTime.UtcNow;

            // 6. Update in repository
            bool updated = await _profileRepository.Update(existingProfile);
            if (!updated)
            {
                throw new InvalidOperationException($"Failed to update encoding profile with ID '{id}'.");
            }

            // 7. Map to DTO and return
            return MapToDto(existingProfile);
        }

        // Soft deletes an encoding profile.
        public async Task<bool> SoftDeleteEncodingProfile(Guid id)
        {
            var deleted = await _profileRepository.SoftDelete(id);
            if (!deleted)
            {
                throw new KeyNotFoundException($"Encoding profile with ID '{id}' not found or could not be soft-deleted.");
            }
            return true;
        }

        // Retrieves a single encoding profile by ID.
        public async Task<EncodingProfileDto?> GetEncodingProfileById(Guid id)
        {
            var profile = await _profileRepository.GetById(id);
            return profile != null ? MapToDto(profile) : null;
        }

        // Retrieves all encoding profiles (for admin view).
        public async Task<IEnumerable<EncodingProfileDto>> GetAllEncodingProfiles()
        {
            var profiles = await _profileRepository.GetAll();
            return profiles.Select(MapToDto).ToList();
        }

        // Retrieves all active encoding profiles (for general user selection).
        public async Task<IEnumerable<EncodingProfileDto>> GetAllActiveEncodingProfiles()
        {
            var profiles = await _profileRepository.GetAllActive();
            return profiles.Select(MapToDto).ToList();
        }

        // Helper method to map an EncodingProfile entity to an EncodingProfileDto.
        private EncodingProfileDto MapToDto(EncodingProfile profile)
        {
            return new EncodingProfileDto
            {
                Id = profile.Id,
                ProfileName = profile.ProfileName,
                Description = profile.Description,
                Resolution = profile.Resolution,
                BitrateKbps = profile.BitrateKbps,
                Format = profile.Format,
                FFmpegArgsTemplate = profile.FFmpegArgsTemplate,
                IsActive = profile.IsActive,
                CreatedAt = profile.CreatedAt,
                LastModifiedAt = profile.LastModifiedAt
            };
        }
    }
}
