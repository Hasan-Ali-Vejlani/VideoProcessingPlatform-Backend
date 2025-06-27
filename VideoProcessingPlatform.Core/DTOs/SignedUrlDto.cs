// VideoProcessingPlatform.Core/DTOs/SignedUrlDto.cs
using System;
using System.Collections.Generic; // Required for List

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for returning a generated signed URL to the frontend.
    public class SignedUrlDto
    {
        // Indicates if the URL generation was successful.
        public bool Success { get; set; }

        // The generated signed URL for video playback.
        public string? Url { get; set; } // Nullable if Success is false

        // A message providing details about the operation (e.g., success, error reason).
        public string? Message { get; set; } // Nullable for success messages or specific errors

        public List<VideoRenditionDto> AvailableRenditions { get; set; } = new List<VideoRenditionDto>();
    }
}
