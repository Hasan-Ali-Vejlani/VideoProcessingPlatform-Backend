// VideoProcessingPlatform.Core/Entities/EncodingProfile.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.Entities
{
    // Represents an administrator-defined encoding profile for video transcoding.
    public class EncodingProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Unique identifier for the encoding profile

        [Required]
        [StringLength(255)]
        public string ProfileName { get; set; } // Unique name of the encoding profile (e.g., "WebHD_720p", "MobileSD")

        [StringLength(1000)] // Allow longer description
        public string? Description { get; set; } // Description of the profile

        [Required]
        [StringLength(50)]
        public string Resolution { get; set; } // e.g., "1280x720", "1920x1080"

        [Required]
        public int BitrateKbps { get; set; } // Target video bitrate in kilobits per second (e.g., 2000 for 2 Mbps)

        [Required]
        [StringLength(50)]
        public string Format { get; set; } // Output format (e.g., "mp4", "hls", "dash")

        // Critical for dynamic FFmpeg commands. This template will contain placeholders
        // like {inputPath} and {outputPath} which will be replaced at runtime by workers.
        [Required]
        [StringLength(4000)] // TEXT equivalent for smaller strings, adjust if very long commands
        public string FFmpegArgsTemplate { get; set; }

        public bool IsActive { get; set; } = true; // For soft deletion: true if active, false if soft-deleted

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
