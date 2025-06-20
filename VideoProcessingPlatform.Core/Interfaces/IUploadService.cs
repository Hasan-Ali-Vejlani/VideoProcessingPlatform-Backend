// VideoProcessingPlatform.Core/Interfaces/IUploadService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for managing the video upload process, including chunking and resume.
    public interface IUploadService
    {
        // Initiates a new upload session, creating initial metadata.
        Task<InitiateUploadResponseDto> InitiateUpload(Guid userId, InitiateUploadRequestDto request);

        // Processes an uploaded chunk, storing it and updating metadata.
        Task<ChunkUploadResponseDto> ProcessChunk(Guid uploadId, int chunkIndex, int totalChunks, Stream chunkData);

        // Retrieves the status of a specific upload, including completed chunks for resume.
        Task<UploadStatusDto?> GetUploadStatus(Guid uploadId);

        // Gets a list of all uploads (or incomplete uploads) for a specific user.
        Task<IEnumerable<UploadStatusDto>> GetUserUploads(Guid userId, string? statusFilter = null);
    }
}
