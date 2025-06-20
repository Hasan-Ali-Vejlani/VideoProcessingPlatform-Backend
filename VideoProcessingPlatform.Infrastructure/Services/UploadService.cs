// VideoProcessingPlatform.Infrastructure/Services/UploadService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // For serializing/deserializing CompletedChunks
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Concrete implementation of IUploadService.
    public class UploadService : IUploadService
    {
        private readonly IUploadMetadataRepository _uploadMetadataRepository;
        private readonly IFileStorageService _fileStorageService;

        public UploadService(IUploadMetadataRepository uploadMetadataRepository, IFileStorageService fileStorageService)
        {
            _uploadMetadataRepository = uploadMetadataRepository;
            _fileStorageService = fileStorageService;
        }

        // Initiates a new upload session or resumes an existing one if possible.
        public async Task<InitiateUploadResponseDto> InitiateUpload(Guid userId, InitiateUploadRequestDto request)
        {
            // First, try to find if an incomplete upload with the same details exists for the user.
            // This is a basic resume check. For more robust resume, you might need to check file hash, etc.
            // For now, let's assume we create a new entry for each initiation,
            // but the frontend will manage the "resume" by providing the existing UploadId.
            // However, the sequence diagram suggests `GetIncompleteUploads`, so let's adjust.
            // For *initiation*, we assume it's a new upload or an explicit re-initiation.
            // The frontend should determine if it's a resume and pass the UploadId to ProcessChunk directly.
            // The InitiateUpload endpoint should primarily be for *new* uploads.

            // Let's refine based on the sequence diagram "ResumeSeq.txt"
            // The resume flow starts by frontend querying for incomplete uploads.
            // If user initiates a *new* upload, we create new metadata.

            // Check for existing uploads by file name and size for potential resume,
            // though the frontend typically handles prompting for resume.
            // For simplicity, this `InitiateUpload` will always create a new entry.
            // The `GetUploadStatus` method will be for checking resume points.

            var newUploadMetadata = new UploadMetadata
            {
                UserId = userId,
                OriginalFileName = request.FileName,
                OriginalFileSize = request.FileSize,
                MimeType = request.MimeType,
                TotalChunks = request.TotalChunks,
                UploadStatus = "InProgress",
                CompletedChunks = JsonSerializer.Serialize(new List<int>()) // Initialize as empty JSON array
            };

            var createdMetadata = await _uploadMetadataRepository.Add(newUploadMetadata);

            return new InitiateUploadResponseDto
            {
                UploadId = createdMetadata.Id,
                Message = "Upload session initiated successfully.",
                Success = true,
                CompletedChunks = new List<int>() // Always empty for a new initiation
            };
        }

        // Processes an uploaded chunk.
        public async Task<ChunkUploadResponseDto> ProcessChunk(Guid uploadId, int chunkIndex, int totalChunks, Stream chunkData)
        {
            var uploadMetadata = await _uploadMetadataRepository.GetById(uploadId);

            if (uploadMetadata == null)
            {
                throw new InvalidOperationException($"Upload session with ID {uploadId} not found.");
            }

            if (uploadMetadata.UploadStatus != "InProgress")
            {
                throw new InvalidOperationException($"Upload session {uploadId} is not in 'InProgress' status. Current status: {uploadMetadata.UploadStatus}");
            }

            // Store the chunk
            await _fileStorageService.StoreChunk(uploadId, chunkIndex, chunkData);

            // Update completed chunks in metadata
            var completedChunksList = JsonSerializer.Deserialize<List<int>>(uploadMetadata.CompletedChunks) ?? new List<int>();

            if (!completedChunksList.Contains(chunkIndex))
            {
                completedChunksList.Add(chunkIndex);
                // Keep the list sorted for consistency, though not strictly required for Contains
                completedChunksList.Sort();
                uploadMetadata.CompletedChunks = JsonSerializer.Serialize(completedChunksList);
                await _uploadMetadataRepository.Update(uploadMetadata);
            }

            bool isUploadCompleted = completedChunksList.Count == totalChunks;
            string? finalStoragePath = null;

            if (isUploadCompleted)
            {
                // All chunks received, merge them
                finalStoragePath = await _fileStorageService.MergeChunks(
                    uploadId, uploadMetadata.OriginalFileName, uploadMetadata.TotalChunks);

                // Update final status and storage path
                uploadMetadata.UploadStatus = "Completed";
                uploadMetadata.OriginalStoragePath = finalStoragePath;
                await _uploadMetadataRepository.Update(uploadMetadata);
            }

            return new ChunkUploadResponseDto
            {
                UploadId = uploadId,
                ChunkIndex = chunkIndex,
                IsCompleted = isUploadCompleted,
                Message = isUploadCompleted ? "File upload completed and merged successfully." : $"Chunk {chunkIndex} processed.",
                FinalStoragePath = finalStoragePath
            };
        }

        // Retrieves the detailed status of a specific upload session.
        public async Task<UploadStatusDto?> GetUploadStatus(Guid uploadId)
        {
            var uploadMetadata = await _uploadMetadataRepository.GetById(uploadId);

            if (uploadMetadata == null)
            {
                return null;
            }

            var completedChunksList = JsonSerializer.Deserialize<List<int>>(uploadMetadata.CompletedChunks) ?? new List<int>();

            return new UploadStatusDto
            {
                Id = uploadMetadata.Id,
                FileName = uploadMetadata.OriginalFileName,
                FileSize = uploadMetadata.OriginalFileSize,
                MimeType = uploadMetadata.MimeType,
                TotalChunks = uploadMetadata.TotalChunks,
                UploadedChunksCount = completedChunksList.Count,
                CompletedChunkIndices = completedChunksList,
                Status = uploadMetadata.UploadStatus,
                UploadedAt = uploadMetadata.UploadedAt,
                FinalStoragePath = uploadMetadata.OriginalStoragePath
            };
        }

        // Retrieves a list of upload statuses for a given user, optionally filtered by status.
        public async Task<IEnumerable<UploadStatusDto>> GetUserUploads(Guid userId, string? statusFilter = null)
        {
            IEnumerable<UploadMetadata> uploads;
            if (!string.IsNullOrEmpty(statusFilter))
            {
                uploads = await _uploadMetadataRepository.GetByUserIdAndStatus(userId, statusFilter);
            }
            else
            {
                uploads = await _uploadMetadataRepository.GetByUserId(userId);
            }

            var uploadStatusDtos = new List<UploadStatusDto>();
            foreach (var uploadMetadata in uploads)
            {
                var completedChunksList = JsonSerializer.Deserialize<List<int>>(uploadMetadata.CompletedChunks) ?? new List<int>();
                uploadStatusDtos.Add(new UploadStatusDto
                {
                    Id = uploadMetadata.Id,
                    FileName = uploadMetadata.OriginalFileName,
                    FileSize = uploadMetadata.OriginalFileSize,
                    MimeType = uploadMetadata.MimeType,
                    TotalChunks = uploadMetadata.TotalChunks,
                    UploadedChunksCount = completedChunksList.Count,
                    CompletedChunkIndices = completedChunksList,
                    Status = uploadMetadata.UploadStatus,
                    UploadedAt = uploadMetadata.UploadedAt,
                    FinalStoragePath = uploadMetadata.OriginalStoragePath
                });
            }

            return uploadStatusDtos;
        }
    }
}
