// VideoProcessingPlatform.Core/DTOs/UpdateTranscodingProgressRequestDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for internal worker use to update transcoding job progress.
    public class UpdateTranscodingProgressRequestDto
    {
        [Required(ErrorMessage = "Progress is required.")]
        [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100.")]
        public int Progress { get; set; }

        [StringLength(1000, ErrorMessage = "Status message cannot exceed 1000 characters.")]
        public string? StatusMessage { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } // e.g., "InProgress", "Completed", "Failed"
    }
}
