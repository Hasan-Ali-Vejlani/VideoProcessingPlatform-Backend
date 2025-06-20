// VideoProcessingPlatform.Core/Entities/UploadMetadata.cs
using System;
using System.Collections.Generic;
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
        public string OriginalFileName { get; set; }

        // The original file size in bytes.
        public long OriginalFileSize { get; set; }

        // MIME type of the uploaded file (e.g., "video/mp4").
        public string MimeType { get; set; }

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
        public string UploadStatus { get; set; } = "InProgress"; // Default status

        // Timestamp when the upload session was initiated.
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for the User who owns this upload.
        // This will be configured in ApplicationDbContext.
        public User User { get; set; }
    }
}