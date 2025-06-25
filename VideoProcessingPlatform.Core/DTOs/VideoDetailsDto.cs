// VideoProcessingPlatform.Core/DTOs/VideoDetailsDto.cs
using System;
using System.Collections.Generic;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO to return comprehensive video details, including selected thumbnail and renditions.
    public class VideoDetailsDto
    {
        public Guid Id { get; set; } // VideoId (UploadMetadata.Id)
        public Guid UserId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public long OriginalFileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public string UploadStatus { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        // Currently selected default thumbnail
        public ThumbnailDto? SelectedThumbnail { get; set; }

        // List of all available renditions for this video
        public List<VideoRenditionDto> AvailableRenditions { get; set; } = new List<VideoRenditionDto>();

        // List of all generated thumbnails for this video (for selection in UI)
        public List<ThumbnailDto> AllThumbnails { get; set; } = new List<ThumbnailDto>();

        // Optional: Latest transcoding job status for quick overview
        public TranscodingJobDto? LatestTranscodingJob { get; set; }
    }
}
