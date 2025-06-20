// VideoProcessingPlatform.Core/DTOs/ChunkUploadResponseDto.cs
using System;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for the response after uploading a file chunk.
    public class ChunkUploadResponseDto
    {
        public Guid UploadId { get; set; } // The ID of the upload session
        public int ChunkIndex { get; set; } // The index of the chunk that was just processed
        public bool IsCompleted { get; set; } // Indicates if the entire file upload is now complete
        public string Message { get; set; } // A status message
        public string? FinalStoragePath { get; set; } // Populated if IsCompleted is true
    }
}
