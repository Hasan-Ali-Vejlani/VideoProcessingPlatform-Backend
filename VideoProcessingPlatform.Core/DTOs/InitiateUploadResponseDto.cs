// VideoProcessingPlatform.Core/DTOs/InitiateUploadResponseDto.cs
using System;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for the response after initiating a new chunked upload.
    public class InitiateUploadResponseDto
    {
        public Guid UploadId { get; set; } // The unique ID for this upload session
        public string Message { get; set; } // A confirmation message
        public bool Success { get; set; } // Indicates if the operation was successful
        public List<int> CompletedChunks { get; set; } = new List<int>(); // List of chunks already uploaded (for resuming)
    }
}
