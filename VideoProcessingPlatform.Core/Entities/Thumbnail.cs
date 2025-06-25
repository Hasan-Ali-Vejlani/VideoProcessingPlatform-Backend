// VideoProcessingPlatform.Core/Entities/Thumbnail.cs
using System;
using System.ComponentModel.DataAnnotations; // Required for attributes like [Required], [StringLength]

namespace VideoProcessingPlatform.Core.Entities
{
    // Represents a single thumbnail image generated for a video.
    public class Thumbnail
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Unique ID for the thumbnail (PK)

        // Foreign key to UploadMetadata, indicating which video this thumbnail belongs to.
        public Guid UploadMetadataId { get; set; }

        // The path or URL where the thumbnail image is stored (e.g., Azure Blob URL, CDN URL).
        [Required]
        [StringLength(512)] // Max length for StoragePath
        public string StoragePath { get; set; } = string.Empty; // Initialize to avoid nullable warnings

        // The timestamp (in seconds from video start) where this thumbnail was extracted.
        public int TimestampSeconds { get; set; }

        // The order in which the thumbnails were generated or should be displayed.
        public int Order { get; set; }

        // Indicates if this is the currently selected default thumbnail for the video.
        public bool IsDefault { get; set; } = false;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow; // Timestamp when thumbnail was generated

        // --- Navigation Property ---
        // The UploadMetadata (video) that this thumbnail belongs to.
        // Marked as null! because EF Core will populate it when loaded from DB.
        public UploadMetadata UploadMetadata { get; set; } = null!;
    }
}
