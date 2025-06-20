// VideoProcessingPlatform.Core/DTOs/ChunkUploadRequestDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // For IFormFile

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for uploading an individual file chunk.
    public class ChunkUploadRequestDto
    {
        [Required]
        public Guid UploadId { get; set; } // The ID of the ongoing upload session

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Chunk index must be non-negative.")]
        public int ChunkIndex { get; set; } // The 0-based index of the current chunk

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Total chunks must be at least 1.")]
        public int TotalChunks { get; set; } // Total number of chunks for the file

        [Required]
        public IFormFile ChunkData { get; set; } // The actual binary data of the chunk
    }
}
