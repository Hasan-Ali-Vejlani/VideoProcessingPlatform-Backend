// VideoProcessingPlatform.Core/DTOs/InitiateUploadRequestDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for initiating a new chunked upload.
    public class InitiateUploadRequestDto
    {
        [Required]
        public string FileName { get; set; } // Original file name

        [Required]
        public long FileSize { get; set; } // Total file size in bytes

        [Required]
        public string MimeType { get; set; } // MIME type of the file (e.g., video/mp4)

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Total chunks must be at least 1.")]
        public int TotalChunks { get; set; } // Total number of chunks
    }
}
