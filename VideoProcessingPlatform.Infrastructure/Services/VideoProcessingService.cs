// VideoProcessingPlatform.Infrastructure/Services/VideoProcessingService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // For includes and no-tracking queries

namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Concrete implementation of IVideoProcessingService.
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly ITranscodingJobRepository _jobRepository;
        private readonly IUploadMetadataRepository _uploadRepository;
        private readonly IEncodingProfileRepository _profileRepository;
        private readonly IMessageQueueService _messageQueueService;
        private readonly IFileStorageService _fileStorageService; // Potentially used for generating playback URLs later

        public VideoProcessingService(
            ITranscodingJobRepository jobRepository,
            IUploadMetadataRepository uploadRepository,
            IEncodingProfileRepository profileRepository,
            IMessageQueueService messageQueueService,
            IFileStorageService fileStorageService)
        {
            _jobRepository = jobRepository;
            _uploadRepository = uploadRepository;
            _profileRepository = profileRepository;
            _messageQueueService = messageQueueService;
            _fileStorageService = fileStorageService;
        }

        // Initiates a new transcoding job.
        public async Task<TranscodingJobInitiatedDto> InitiateTranscoding(Guid userId, InitiateTranscodingRequestDto request)
        {
            // 1. Validate Uploaded Video (UploadMetadata) existence and status
            var uploadMetadata = await _uploadRepository.GetById(request.VideoId);
            if (uploadMetadata == null)
            {
                throw new KeyNotFoundException($"Uploaded video with ID '{request.VideoId}' not found.");
            }
            if (uploadMetadata.UploadStatus != "Completed")
            {
                throw new InvalidOperationException($"Video upload for ID '{request.VideoId}' is not yet completed. Current status: {uploadMetadata.UploadStatus}");
            }
            if (uploadMetadata.UserId != userId)
            {
                throw new UnauthorizedAccessException($"User is not authorized to transcode video with ID '{request.VideoId}'.");
            }

            // 2. Validate Encoding Profile existence and activeness
            var encodingProfile = await _profileRepository.GetById(request.EncodingProfileId);
            if (encodingProfile == null || !encodingProfile.IsActive)
            {
                throw new KeyNotFoundException($"Encoding profile with ID '{request.EncodingProfileId}' not found or is inactive.");
            }

            // 3. Create a new TranscodingJob entry in the database
            var newJob = new TranscodingJob
            {
                UploadMetadataId = request.VideoId,
                UserId = userId,
                EncodingProfileId = request.EncodingProfileId,
                SourceStoragePath = uploadMetadata.OriginalStoragePath, // Use the path from the completed upload
                Status = "Queued", // Initial status
                Progress = 0,
                StatusMessage = "Job created and queued for processing.",
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            var createdJob = await _jobRepository.Add(newJob);

            // 4. Create a message for the Transcoding Worker and publish to Message Queue
            var jobMessage = new TranscodingJobMessage
            {
                TranscodingJobId = createdJob.Id,
                UploadMetadataId = createdJob.UploadMetadataId,
                SourceVideoPath = createdJob.SourceStoragePath,
                FFmpegArgsTemplate = encodingProfile.FFmpegArgsTemplate,
                TargetFormat = encodingProfile.Format,
                TargetResolution = encodingProfile.Resolution,
                TargetBitrateKbps = encodingProfile.BitrateKbps,
                ApplyWatermark = false, // Placeholder: implement logic for this later
                ApplyDRM = false // Placeholder: implement logic for this later (CENC)
            };

            await _messageQueueService.PublishTranscodingJob(jobMessage);

            return new TranscodingJobInitiatedDto
            {
                JobId = createdJob.Id,
                Message = "Transcoding job queued successfully.",
                Status = createdJob.Status
            };
        }

        // Retrieves the status of a specific transcoding job.
        public async Task<TranscodingJobDto?> GetTranscodingJobStatus(Guid jobId)
        {
            var job = await _jobRepository.GetById(jobId);

            if (job == null)
            {
                return null;
            }

            // Ensure UploadMetadata and EncodingProfile are loaded for mapping
            if (job.UploadMetadata == null)
            {
                job.UploadMetadata = await _uploadRepository.GetById(job.UploadMetadataId) ?? throw new InvalidOperationException("UploadMetadata not found for transcoding job.");
            }
            if (job.EncodingProfile == null)
            {
                job.EncodingProfile = await _profileRepository.GetById(job.EncodingProfileId) ?? throw new InvalidOperationException("EncodingProfile not found for transcoding job.");
            }

            return MapToDto(job);
        }

        // Retrieves all transcoding jobs for a given user.
        public async Task<IEnumerable<TranscodingJobDto>> GetUserTranscodingJobs(Guid userId)
        {
            var jobs = await _jobRepository.GetByUserId(userId);
            return jobs.Select(MapToDto).ToList();
        }

        // Retrieves all transcoding jobs for a specific uploaded video.
        public async Task<IEnumerable<TranscodingJobDto>> GetTranscodingJobsForVideo(Guid videoId)
        {
            var jobs = await _jobRepository.GetByUploadMetadataId(videoId);
            return jobs.Select(MapToDto).ToList();
        }

        // Updates job progress from the worker (intended for internal use by worker)
        public async Task UpdateTranscodingJobProgress(Guid jobId, int progress, string statusMessage, string status)
        {
            var job = await _jobRepository.GetById(jobId);
            if (job == null)
            {
                // Log: Job not found for progress update
                return;
            }

            job.Progress = progress;
            job.StatusMessage = statusMessage;
            job.Status = status;
            job.LastUpdatedAt = DateTime.UtcNow;

            await _jobRepository.Update(job);
        }

        // Completes a job from the worker, adding renditions (intended for internal use by worker)
        public async Task CompleteTranscodingJob(Guid jobId, List<VideoRenditionDto> renditionDtos)
        {
            var job = await _jobRepository.GetById(jobId);
            if (job == null)
            {
                // Log: Job not found for completion
                return;
            }

            job.Status = "Completed";
            job.Progress = 100;
            job.StatusMessage = "Transcoding completed successfully.";
            job.LastUpdatedAt = DateTime.UtcNow;

            // Add renditions
            foreach (var renditionDto in renditionDtos)
            {
                var rendition = new VideoRendition
                {
                    TranscodingJobId = jobId,
                    RenditionType = renditionDto.RenditionType,
                    StoragePath = renditionDto.StoragePath,
                    IsEncrypted = renditionDto.IsEncrypted,
                    PlaybackUrl = renditionDto.PlaybackUrl, // Will be generated by CDN service later
                    GeneratedAt = DateTime.UtcNow
                };
                await _jobRepository.AddRendition(rendition);
            }

            await _jobRepository.Update(job);
        }

        // Marks a job as failed (intended for internal use by worker)
        public async Task FailTranscodingJob(Guid jobId, string errorMessage)
        {
            var job = await _jobRepository.GetById(jobId);
            if (job == null)
            {
                // Log: Job not found for failure
                return;
            }

            job.Status = "Failed";
            job.Progress = job.Progress; // Keep last known progress
            job.StatusMessage = errorMessage;
            job.LastUpdatedAt = DateTime.UtcNow;

            await _jobRepository.Update(job);
        }

        // Helper method to map TranscodingJob entity to TranscodingJobDto.
        private TranscodingJobDto MapToDto(TranscodingJob job)
        {
            return new TranscodingJobDto
            {
                Id = job.Id,
                UserId = job.UserId, // <--- ADDED THIS LINE: Populate UserId from entity
                VideoId = job.UploadMetadataId,
                OriginalFileName = job.UploadMetadata?.OriginalFileName ?? "N/A", // Use null conditional operator
                EncodingProfileName = job.EncodingProfile?.ProfileName ?? "N/A", // Use null conditional operator
                Status = job.Status,
                Progress = job.Progress,
                StatusMessage = job.StatusMessage,
                CreatedAt = job.CreatedAt,
                LastUpdatedAt = job.LastUpdatedAt,
                Renditions = job.VideoRenditions?.Select(vr => new VideoRenditionDto
                {
                    Id = vr.Id,
                    TranscodingJobId = vr.TranscodingJobId,
                    RenditionType = vr.RenditionType,
                    StoragePath = vr.StoragePath,
                    IsEncrypted = vr.IsEncrypted,
                    PlaybackUrl = vr.PlaybackUrl,
                    GeneratedAt = vr.GeneratedAt
                }).ToList() ?? new List<VideoRenditionDto>()
            };
        }
    }
}
