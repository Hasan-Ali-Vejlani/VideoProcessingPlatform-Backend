// VideoProcessingPlatform.Core/Entities/VideoRendition.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.Entities
{
    // Represents a single output rendition (e.g., 720p HLS, 1080p DASH) generated from a transcoding job.
    public class VideoRendition
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Unique ID for the video rendition (PK)

        // ID of the transcoding job this rendition belongs to (FK to TranscodingJob.Id)
        public Guid TranscodingJobId { get; set; }

        // Type of rendition (e.g., "HLS_720p", "DASH_1080p", "MP4_SD")
        [Required]
        [StringLength(100)]
        public string RenditionType { get; set; }

        // Path/URI where this specific rendition is stored (e.g., Azure Blob URL, CDN path)
        [Required]
        [StringLength(512)]
        public string StoragePath { get; set; }

        // This is important for selecting the correct playback quality.
        [Required]
        [StringLength(50)] // Sufficient length for resolution strings
        public string Resolution { get; set; } = string.Empty;

        // The video bitrate in kilobits per second (Kbps).
        // Also crucial for quality selection and sorting.
        [Required] // Bitrate should always be provided
        public int BitrateKbps { get; set; }

        // Whether this rendition is encrypted (for CENC support)
        public bool IsEncrypted { get; set; } = false;

        // Playback URL (e.g., CDN URL, might be dynamic via signed URLs)
        // This can be null initially and populated by a CDN service later.
        [StringLength(512)]
        public string? PlaybackUrl { get; set; }

        // Timestamp when this rendition was generated/completed
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Property ---
        public TranscodingJob TranscodingJob { get; set; } = null!; // The job that produced this rendition
    }
}
