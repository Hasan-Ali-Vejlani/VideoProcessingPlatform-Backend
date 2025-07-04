﻿// VideoProcessingPlatform.Core/DTOs/TranscodingJobDTOs.cs
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
        public Guid UserId { get; set; }
        public Guid VideoId { get; set; }
        public string OriginalFileName { get; set; } // From UploadMetadata
        public string EncodingProfileName { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public List<VideoRenditionDto> Renditions { get; set; } = new List<VideoRenditionDto>();
    }
}
