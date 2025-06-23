// VideoProcessingPlatform.Core/DTOs/TranscodingJobDTOs.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for initiating a transcoding job from the API.
    public class InitiateTranscodingRequestDto
    {
        [Required(ErrorMessage = "Video ID (Upload Metadata ID) is required.")]
        public Guid VideoId { get; set; } // The ID of the original uploaded video

        [Required(ErrorMessage = "Encoding Profile ID is required.")]
        public Guid EncodingProfileId { get; set; } // The ID of the chosen encoding profile
    }

    // DTO for response after a transcoding job has been initiated/queued.
    public class TranscodingJobInitiatedDto
    {
        public Guid JobId { get; set; } // The ID of the newly created transcoding job
        public string Message { get; set; } // Status message (e.g., "Job queued successfully.")
        public string Status { get; set; } // Current status (e.g., "Queued")
    }

    // DTO for displaying a transcoding job's status to the user.
    public class TranscodingJobDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } // <--- ADDED THIS LINE: The ID of the user who owns this job
        public Guid VideoId { get; set; } // Original UploadMetadata ID
        public string OriginalFileName { get; set; } // From UploadMetadata
        public string EncodingProfileName { get; set; } // Name of the profile used
        public string Status { get; set; }
        public int Progress { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public List<VideoRenditionDto> Renditions { get; set; } = new List<VideoRenditionDto>();
    }

    // DTO for representing a single video rendition.
    public class VideoRenditionDto
    {
        public Guid Id { get; set; }
        public Guid TranscodingJobId { get; set; }
        public string RenditionType { get; set; }
        public string StoragePath { get; set; }
        public bool IsEncrypted { get; set; }
        public string? PlaybackUrl { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    // DTO for general video details, potentially including its renditions.
    // This could be used for a 'My Videos' or 'Video Details' page later.
    public class VideoDetailsDto
    {
        public Guid VideoId { get; set; }
        public string FileName { get; set; }
        public string OriginalStoragePath { get; set; }
        public string UploadStatus { get; set; }
        public DateTime UploadedAt { get; set; }
        public List<TranscodingJobDto> TranscodingJobs { get; set; } = new List<TranscodingJobDto>();
    }
}
