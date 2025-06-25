// VideoProcessingPlatform.Core/Entities/TranscodingJob.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoProcessingPlatform.Core.Entities
{
    // Represents a single video transcoding job.
    public class TranscodingJob
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Unique ID for the transcoding job (PK)

        // ID of the uploaded video that is being transcoded (FK to UploadMetadata.Id)
        public Guid UploadMetadataId { get; set; }

        // ID of the user who initiated this transcoding job (FK to User.Id)
        public Guid UserId { get; set; }

        // ID of the encoding profile used for this job (FK to EncodingProfile.Id)
        public Guid EncodingProfileId { get; set; }

        // Path to the original video file in storage (from UploadMetadata.OriginalStoragePath)
        [Required]
        [StringLength(512)]
        public string SourceStoragePath { get; set; } = string.Empty; // Initialize to avoid nullable warnings

        // Current status of the transcoding job (e.g., "Queued", "InProgress", "Completed", "Failed")
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Queued"; // Default status

        // Progress percentage (0-100)
        public int Progress { get; set; } = 0; // Default progress

        // Optional message for status updates or errors
        [Column(TypeName = "nvarchar(max)")]
        public string? StatusMessage { get; set; }

        // Timestamp when the job was created/queued
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Timestamp when the job status was last updated
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // --- NEW: Properties to carry encoding profile details directly for worker (REQUIRED by ApplicationDbContext) ---
        // These are denormalized for efficient message queueing and worker processing without extra DB lookups
        [Required]
        [StringLength(255)]
        public string EncodingProfileName { get; set; } = string.Empty; // Name of the profile used

        [Required]
        [StringLength(50)]
        public string TargetResolution { get; set; } = string.Empty; // e.g., "1920x1080"

        [Required]
        public int TargetBitrateKbps { get; set; }

        [Required]
        [StringLength(50)]
        public string TargetFormat { get; set; } = string.Empty; // e.g., "mp4", "hls"

        [Required]
        [StringLength(4000)]
        public string FFmpegArgsTemplate { get; set; } = string.Empty;

        public bool ApplyDRM { get; set; } = false; // Whether DRM should be applied for this job

        // --- Navigation Properties ---
        public UploadMetadata UploadMetadata { get; set; } = null!; // The original video metadata (Ensuring it's non-nullable for EF Core)
        public User User { get; set; } = null!; // The user who initiated (Ensuring it's non-nullable for EF Core)
        public EncodingProfile EncodingProfile { get; set; } = null!; // The profile used for transcoding (Ensuring it's non-nullable for EF Core)

        // Collection of renditions produced by this job (one-to-many relationship)
        public ICollection<VideoRendition> VideoRenditions { get; set; } = new List<VideoRendition>();
    }
}
