// VideoProcessingPlatform.Core/DTOs/VideoRenditionDto.cs
using System;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO representing details of a single video rendition after transcoding.
    public class VideoRenditionDto
    {
        // Unique identifier for the rendition.
        public Guid Id { get; set; }

        // ID of the transcoding job this rendition belongs to (FK)
        public Guid TranscodingJobId { get; set; }

        // The type or quality of the rendition (e.g., "HLS_720p", "DASH_1080p", "MP4_360p").
        public string RenditionType { get; set; } = string.Empty;

        // The path or URI where this specific rendition is stored (e.g., in Azure Blob Storage).
        public string StoragePath { get; set; } = string.Empty;

        // The resolution of the video (e.g., "1280x720", "640x360").
        public string Resolution { get; set; } = string.Empty;

        // The video bitrate in kilobits per second (Kbps).
        public int BitrateKbps { get; set; }

        // Indicates whether this rendition is encrypted (e.g., for DRM).
        public bool IsEncrypted { get; set; }

        // The direct playback URL for this rendition (this would be the signed CDN URL).
        // This field will be populated by the VideoPlaybackService.
        public string? PlaybackUrl { get; set; } // Nullable if not yet generated or if an error occurs

        // Timestamp when this rendition was generated/completed
        public DateTime GeneratedAt { get; set; }
    }
}
