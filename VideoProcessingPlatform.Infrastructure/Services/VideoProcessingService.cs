// VideoProcessingPlatform.Infrastructure/Services/VideoProcessingService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoProcessingPlatform.Core.Extensions; // --- NEW: Add this using statement ---

namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Concrete implementation of IVideoProcessingService.
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly ITranscodingJobRepository _jobRepository;
        private readonly IUploadMetadataRepository _uploadRepository;
        private readonly IEncodingProfileRepository _profileRepository;
        private readonly IMessageQueueService _messageQueueService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IThumbnailService _thumbnailService;
        private readonly ILogger<VideoProcessingService> _logger;

        public VideoProcessingService(
            ITranscodingJobRepository jobRepository,
            IUploadMetadataRepository uploadRepository,
            IEncodingProfileRepository profileRepository,
            IMessageQueueService messageQueueService,
            IFileStorageService fileStorageService,
            IThumbnailService thumbnailService,
            ILogger<VideoProcessingService> logger)
        {
            _jobRepository = jobRepository;
            _uploadRepository = uploadRepository;
            _profileRepository = profileRepository;
            _messageQueueService = messageQueueService;
            _fileStorageService = fileStorageService;
            _thumbnailService = thumbnailService;
            _logger = logger;
        }

        // Initiates a new transcoding job.
        public async Task<TranscodingJobInitiatedDto> InitiateTranscoding(Guid userId, InitiateTranscodingRequestDto request)
        {
            var uploadMetadata = await _uploadRepository.GetById(request.VideoId);
            if (uploadMetadata == null)
            {
                _logger.LogWarning($"Attempt to initiate transcoding for non-existent video ID: {request.VideoId}");
                throw new KeyNotFoundException($"Uploaded video with ID '{request.VideoId}' not found.");
            }
            if (uploadMetadata.UploadStatus != "Completed")
            {
                _logger.LogWarning($"Video upload for ID '{request.VideoId}' not completed. Current status: {uploadMetadata.UploadStatus}");
                throw new InvalidOperationException($"Video upload for ID '{request.VideoId}' is not yet completed. Current status: {uploadMetadata.UploadStatus}");
            }
            if (uploadMetadata.UserId != userId)
            {
                _logger.LogWarning($"User {userId} attempted to transcode video {request.VideoId} belonging to another user.");
                throw new UnauthorizedAccessException($"User is not authorized to transcode video with ID '{request.VideoId}'.");
            }

            var encodingProfile = await _profileRepository.GetById(request.EncodingProfileId);
            if (encodingProfile == null || !encodingProfile.IsActive)
            {
                _logger.LogWarning($"Encoding profile with ID '{request.EncodingProfileId}' not found or is inactive.");
                throw new KeyNotFoundException($"Encoding profile with ID '{request.EncodingProfileId}' not found or is inactive.");
            }

            var newJob = new TranscodingJob
            {
                UploadMetadataId = request.VideoId,
                UserId = userId,
                EncodingProfileId = request.EncodingProfileId,
                SourceStoragePath = uploadMetadata.OriginalStoragePath,
                Status = "Queued",
                Progress = 0,
                StatusMessage = "Job created and queued for processing.",
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                EncodingProfileName = encodingProfile.ProfileName,
                TargetResolution = encodingProfile.Resolution,
                TargetBitrateKbps = encodingProfile.BitrateKbps,
                TargetFormat = encodingProfile.Format,
                FFmpegArgsTemplate = encodingProfile.FFmpegArgsTemplate,
                ApplyDRM = encodingProfile.ApplyDRM
            };

            var createdJob = await _jobRepository.Add(newJob);
            _logger.LogInformation($"Transcoding job {createdJob.Id} created for video {request.VideoId} with profile {request.EncodingProfileId}.");

            var jobMessage = new TranscodingJobMessage
            {
                TranscodingJobId = createdJob.Id,
                UploadMetadataId = createdJob.UploadMetadataId,
                SourceVideoPath = createdJob.SourceStoragePath,
                FFmpegArgsTemplate = createdJob.FFmpegArgsTemplate,
                TargetFormat = createdJob.TargetFormat,
                TargetResolution = createdJob.TargetResolution,
                TargetBitrateKbps = createdJob.TargetBitrateKbps,
                ApplyWatermark = false,
                ApplyDRM = createdJob.ApplyDRM
            };

            await _messageQueueService.PublishTranscodingJob(jobMessage);
            _logger.LogInformation($"Transcoding job {createdJob.Id} message published to queue.");

            return new TranscodingJobInitiatedDto
            {
                JobId = createdJob.Id,
                Message = "Transcoding job queued successfully.",
                Status = createdJob.Status
            };
        }

        public async Task<TranscodingJobDto?> GetTranscodingJobStatus(Guid jobId)
        {
            var job = await _jobRepository.GetById(jobId);

            if (job == null)
            {
                return null;
            }
            return MapToDto(job);
        }

        public async Task<IEnumerable<TranscodingJobDto>> GetUserTranscodingJobs(Guid userId)
        {
            var jobs = await _jobRepository.GetByUserId(userId);
            return jobs.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<TranscodingJobDto>> GetTranscodingJobsForVideo(Guid videoId)
        {
            var jobs = await _jobRepository.GetByUploadMetadataId(videoId);
            return jobs.Select(MapToDto).ToList();
        }

        public async Task UpdateTranscodingJobProgress(Guid jobId, int progress, string statusMessage, string status)
        {
            var job = await _jobRepository.GetById(jobId);
            if (job == null)
            {
                _logger.LogWarning($"Job {jobId} not found for progress update.");
                return;
            }

            job.Progress = progress;
            job.StatusMessage = statusMessage;
            job.Status = status;
            job.LastUpdatedAt = DateTime.UtcNow;

            await _jobRepository.Update(job);
            _logger.LogInformation($"Transcoding job {jobId} progress updated to {progress}% ({status}).");
        }

        public async Task CompleteTranscodingJob(Guid jobId, List<VideoRenditionDto> renditionDtos)
        {
            var job = await _jobRepository.GetById(jobId);
            if (job == null)
            {
                _logger.LogWarning($"Job {jobId} not found for completion.");
                return;
            }

            job.Status = "Completed";
            job.Progress = 100;
            job.StatusMessage = "Transcoding completed successfully.";
            job.LastUpdatedAt = DateTime.UtcNow;

            foreach (var renditionDto in renditionDtos)
            {
                var rendition = new VideoRendition
                {
                    TranscodingJobId = jobId,
                    RenditionType = renditionDto.RenditionType,
                    StoragePath = renditionDto.StoragePath,
                    IsEncrypted = renditionDto.IsEncrypted,
                    Resolution = renditionDto.Resolution,
                    BitrateKbps = renditionDto.BitrateKbps,
                    PlaybackUrl = renditionDto.PlaybackUrl,
                    GeneratedAt = DateTime.UtcNow
                };
                job.VideoRenditions.Add(rendition);
            }

            await _jobRepository.Update(job);
            _logger.LogInformation($"Transcoding job {jobId} marked as completed with {renditionDtos.Count} renditions.");
        }

        public async Task FailTranscodingJob(Guid jobId, string errorMessage)
        {
            var job = await _jobRepository.GetById(jobId);
            if (job == null)
            {
                _logger.LogWarning($"Job {jobId} not found for failure.");
                return;
            }

            job.Status = "Failed";
            job.Progress = job.Progress;
            job.StatusMessage = errorMessage;
            job.LastUpdatedAt = DateTime.UtcNow;

            await _jobRepository.Update(job);
            _logger.LogError($"Transcoding job {jobId} failed: {errorMessage}");
        }

        // --- Implement GetVideoDetailsAsync for frontend display ---
        public async Task<VideoDetailsDto?> GetVideoDetailsAsync(Guid videoId, Guid userId)
        {
            var uploadMetadata = await _uploadRepository.GetByIdWithThumbnails(videoId);
            if (uploadMetadata == null || uploadMetadata.UserId != userId)
            {
                _logger.LogWarning($"Video details requested for non-existent video {videoId} or unauthorized user {userId}.");
                return null;
            }

            var latestJob = await _jobRepository.GetLatestJobForVideo(videoId);

            // Generate signed URLs for thumbnails
            var thumbnailsWithSignedUrls = await Task.WhenAll(
            uploadMetadata.Thumbnails
                .OrderBy(t => t.Order)
                .Select(async t =>
                {
                    var dto = t.ToDto();
                    var blobPath = ExtractBlobPath(t.StoragePath);
                    dto.SignedUrl = await _fileStorageService.GenerateBlobSasUrl(blobPath, TimeSpan.FromHours(1));
                    return dto;
                })
             );

            var selectedThumbnailEntity = uploadMetadata.Thumbnails.FirstOrDefault(t => t.IsDefault)
                                       ?? uploadMetadata.Thumbnails.FirstOrDefault();

            ThumbnailDto? selectedThumbnailDto = null;
            if (selectedThumbnailEntity != null)
            {
                selectedThumbnailDto = selectedThumbnailEntity.ToDto();
                var selectedBlobPath = ExtractBlobPath(selectedThumbnailEntity.StoragePath);
                selectedThumbnailDto.SignedUrl = await _fileStorageService.GenerateBlobSasUrl(selectedBlobPath, TimeSpan.FromHours(1));
            }

            var videoDetailsDto = new VideoDetailsDto
            {
                Id = uploadMetadata.Id,
                UserId = uploadMetadata.UserId,
                OriginalFileName = uploadMetadata.OriginalFileName,
                OriginalFileSize = uploadMetadata.OriginalFileSize,
                MimeType = uploadMetadata.MimeType,
                UploadStatus = uploadMetadata.UploadStatus,
                UploadedAt = uploadMetadata.UploadedAt,
                LastUpdatedAt = uploadMetadata.LastUpdatedAt,

                SelectedThumbnail = selectedThumbnailDto,
                AllThumbnails = thumbnailsWithSignedUrls.ToList(),

                AvailableRenditions = latestJob?.VideoRenditions
                    .OrderByDescending(vr => vr.BitrateKbps)
                    .Select(vr => vr.ToDto())
                    .ToList() ?? new List<VideoRenditionDto>(),

                LatestTranscodingJob = latestJob != null ? MapToDto(latestJob) : null
            };

            return videoDetailsDto;
        }

        // --- Implement GetAllThumbnailsForVideoAsync ---
        public async Task<IEnumerable<ThumbnailDto>> GetAllThumbnailsForVideoAsync(Guid videoId)
        {
            var thumbnails = await _thumbnailService.GetThumbnailsForVideoAsync(videoId);

            foreach (var thumbnail in thumbnails)
            {
                var blobPath = ExtractBlobPath(thumbnail.StoragePath);
                thumbnail.SignedUrl = await _fileStorageService.GenerateBlobSasUrl(blobPath, TimeSpan.FromHours(1));
            }

            return thumbnails;
        }

        // --- Implement SetDefaultThumbnailAsync ---
        public async Task<SetSelectedThumbnailResponseDto> SetDefaultThumbnailAsync(Guid videoId, Guid thumbnailId)
        {
            return await _thumbnailService.SetDefaultThumbnailAsync(videoId, thumbnailId);
        }

        // Helper method to map TranscodingJob entity to TranscodingJobDto.
        private TranscodingJobDto MapToDto(TranscodingJob job)
        {
            return new TranscodingJobDto
            {
                Id = job.Id,
                UserId = job.UserId,
                VideoId = job.UploadMetadataId,
                OriginalFileName = job.UploadMetadata?.OriginalFileName ?? "N/A",
                EncodingProfileName = job.EncodingProfile?.ProfileName ?? job.EncodingProfileName,
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
                    GeneratedAt = vr.GeneratedAt,
                    Resolution = vr.Resolution,
                    BitrateKbps = vr.BitrateKbps
                }).ToList() ?? new List<VideoRenditionDto>()
            };
        }
        private string ExtractBlobPath(string fullUrl)
        {
            var uri = new Uri(fullUrl);
            return uri.AbsolutePath.TrimStart('/'); // removes the leading '/'
        }

        public async Task<bool> ThumbnailsExistForVideoAsync(Guid videoId)
        {
            var thumbnails = await _thumbnailService.GetThumbnailsForVideoAsync(videoId);
            return thumbnails.Any();
        }

    }
}
