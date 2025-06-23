// VideoProcessingPlatform.Core/DTOs/EncodingProfileDTOs.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for returning encoding profile details.
    public class EncodingProfileDto
    {
        public Guid Id { get; set; }
        public string ProfileName { get; set; }
        public string? Description { get; set; }
        public string Resolution { get; set; }
        public int BitrateKbps { get; set; }
        public string Format { get; set; }
        public string FFmpegArgsTemplate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }

    // DTO for creating a new encoding profile.
    public class CreateEncodingProfileDto
    {
        [Required(ErrorMessage = "Profile name is required.")]
        [StringLength(255, ErrorMessage = "Profile name cannot exceed 255 characters.")]
        public string ProfileName { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Resolution is required (e.g., '1280x720').")]
        [StringLength(50, ErrorMessage = "Resolution cannot exceed 50 characters.")]
        public string Resolution { get; set; }

        [Required(ErrorMessage = "Bitrate (Kbps) is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Bitrate must be a positive integer.")]
        public int BitrateKbps { get; set; }

        [Required(ErrorMessage = "Format is required (e.g., 'mp4', 'hls').")]
        [StringLength(50, ErrorMessage = "Format cannot exceed 50 characters.")]
        public string Format { get; set; }

        // The raw FFmpeg arguments template will be provided by the admin.
        // It will be validated and potentially built into FFmpegArgsTemplate by the service.
        [Required(ErrorMessage = "FFmpeg arguments template is required.")]
        [StringLength(4000, ErrorMessage = "FFmpeg arguments template cannot exceed 4000 characters.")]
        public string FFmpegArgsTemplate { get; set; }
    }

    // DTO for updating an existing encoding profile.
    public class UpdateEncodingProfileDto
    {
        [Required(ErrorMessage = "Profile name is required.")]
        [StringLength(255, ErrorMessage = "Profile name cannot exceed 255 characters.")]
        public string ProfileName { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Resolution is required (e.g., '1280x720').")]
        [StringLength(50, ErrorMessage = "Resolution cannot exceed 50 characters.")]
        public string Resolution { get; set; }

        [Required(ErrorMessage = "Bitrate (Kbps) is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Bitrate must be a positive integer.")]
        public int BitrateKbps { get; set; }

        [Required(ErrorMessage = "Format is required (e.g., 'mp4', 'hls').")]
        [StringLength(50, ErrorMessage = "Format cannot exceed 50 characters.")]
        public string Format { get; set; }

        [Required(ErrorMessage = "FFmpeg arguments template is required.")]
        [StringLength(4000, ErrorMessage = "FFmpeg arguments template cannot exceed 4000 characters.")]
        public string FFmpegArgsTemplate { get; set; }

        public bool IsActive { get; set; } = true; // Allow admin to activate/deactivate
    }
}
