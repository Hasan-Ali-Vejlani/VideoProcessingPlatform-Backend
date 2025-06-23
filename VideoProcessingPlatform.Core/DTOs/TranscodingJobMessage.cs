// VideoProcessingPlatform.Core/DTOs/TranscodingJobMessage.cs
using System;
using System.Collections.Generic;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO representing a message to be sent to the message queue for a transcoding worker.
    public class TranscodingJobMessage
    {
        public Guid TranscodingJobId { get; set; } // The ID of the job to process
        public Guid UploadMetadataId { get; set; } // The ID of the original uploaded video
        public string SourceVideoPath { get; set; } // Path to the original video file in storage
        public string FFmpegArgsTemplate { get; set; } // The FFmpeg command template from the profile
        public string TargetFormat { get; set; } // e.g., "hls", "dash", "mp4" (from EncodingProfile.Format)
        public string TargetResolution { get; set; } // e.g., "1920x1080" (from EncodingProfile.Resolution)
        public int TargetBitrateKbps { get; set; } // e.g., 2000 (from EncodingProfile.BitrateKbps)
        public bool ApplyWatermark { get; set; } // Example: if watermarking is enabled
        public bool ApplyDRM { get; set; } // Example: if DRM is enabled (for CENC)

        // Additional metadata that worker might need, e.g., for reporting progress or specific renditions
        public Dictionary<string, string> AdditionalMetadata { get; set; } = new Dictionary<string, string>();
    }
}
