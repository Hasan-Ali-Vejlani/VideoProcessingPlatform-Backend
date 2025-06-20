// VideoProcessingPlatform.Core/DTOs/UploadStatusDto.cs
using System;
using System.Collections.Generic;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO to represent the status of an ongoing or completed upload, used for the frontend.
    public class UploadStatusDto
    {
        public Guid Id { get; set; } // Unique ID for the upload
        public string FileName { get; set; } // Original file name
        public long FileSize { get; set; } // Total file size
        public string MimeType { get; set; } // MIME type
        public int TotalChunks { get; set; } // Total chunks expected
        public int UploadedChunksCount { get; set; } // Number of chunks already uploaded
        public List<int> CompletedChunkIndices { get; set; } = new List<int>(); // List of uploaded chunk indices
        public string Status { get; set; } // Current status (e.g., "InProgress", "Completed", "Failed")
        public DateTime UploadedAt { get; set; } // When the upload was initiated
        public string? FinalStoragePath { get; set; } // Path if completed
        public double ProgressPercentage => TotalChunks > 0 ? (double)UploadedChunksCount / TotalChunks * 100 : 0;
    }
}
