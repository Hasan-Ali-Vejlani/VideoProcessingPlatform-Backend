// VideoProcessingPlatform.Core/Entities/UploadMetadata.cs
using System;
using System.Collections.Generic; // Required for ICollection
using System.ComponentModel.DataAnnotations; // Required for attributes like [Required], [StringLength]
using System.Linq; // Needed for Contains and other LINQ operations if using list/array

namespace VideoProcessingPlatform.Core.Entities
{
    // Represents the metadata for a video upload session, tracking progress for chunked uploads.
    public class UploadMetadata
    {
        // Unique ID for the upload session (also serves as the VideoId once merged).
        // This is a primary key.
        public Guid Id { get; set; } = Guid.NewGuid();

        // ID of the user who initiated the upload. Foreign key to User entity.
        public Guid UserId { get; set; }

        // The original file name provided by the user.
        [Required] // Ensuring consistency with database mapping
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty; // Initialized to avoid nullable warnings

        // The original file size in bytes.
        public long OriginalFileSize { get; set; }

        // MIME type of the uploaded file (e.g., "video/mp4").
        [Required] // Ensuring consistency with database mapping
        [StringLength(100)] // Adjusted length for MIME types, if needed. Keep 50 if that's current DB.
        public string MimeType { get; set; } = string.Empty; // Initialized to avoid nullable warnings

        // Total number of chunks the original file was divided into.
        public int TotalChunks { get; set; }

        // A serialized representation (e.g., JSON string) of the indices of chunks that have
        // been successfully uploaded. This allows for resuming uploads.
        // It's stored as TEXT/NVARCHAR(MAX) in DB as per ERD.
        public string CompletedChunks { get; set; } = "[]"; // Initialize as empty JSON array

        // Path/URI where the original, merged uploaded video file is stored (e.g., Azure Blob URL, local path).
        // This will be populated once the upload is completed and chunks are merged.
        public string? OriginalStoragePath { get; set; } // Nullable until merge is complete

        // Current status of the upload (e.g., 'InProgress', 'Completed', 'Failed', 'Cancelled').
        [Required] // Ensuring consistency with database mapping
        [StringLength(50)]
        public string UploadStatus { get; set; } = "InProgress"; // Default status

        // Timestamp when the upload session was initiated.
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Timestamp when the upload metadata or status was last updated
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // Stores the URL of the thumbnail selected by the user as the primary for this video
        [StringLength(512)] // Ensure adequate length for URLs
        public string? SelectedThumbnailUrl { get; set; } // Can be null initially

        // Navigation Property for the User who owns this upload
        public User User { get; set; } = null!; // Initialized to null! to indicate it will be populated by EF Core

        // Navigation collection for related transcoding jobs
        public ICollection<TranscodingJob> TranscodingJobs { get; set; } = new List<TranscodingJob>();

        // Navigation collection for generated thumbnails (for this video)
        public ICollection<Thumbnail> Thumbnails { get; set; } = new List<Thumbnail>();
    }
}
