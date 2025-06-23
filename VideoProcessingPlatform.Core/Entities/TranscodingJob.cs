// VideoProcessingPlatform.Core/Entities/TranscodingJob.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        public string SourceStoragePath { get; set; }

        // Current status of the transcoding job (e.g., "Queued", "InProgress", "Completed", "Failed")
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Queued"; // Default status

        // Progress percentage (0-100)
        public int Progress { get; set; } = 0; // Default progress

        // Optional message for status updates or errors
        public string? StatusMessage { get; set; }

        // Timestamp when the job was created/queued
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Timestamp when the job status was last updated
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        public UploadMetadata UploadMetadata { get; set; } // The original video metadata
        public User User { get; set; } // The user who initiated
        public EncodingProfile EncodingProfile { get; set; } // The profile used for transcoding

        // Collection of renditions produced by this job (one-to-many relationship)
        public ICollection<VideoRendition> VideoRenditions { get; set; } = new List<VideoRendition>();
    }
}
