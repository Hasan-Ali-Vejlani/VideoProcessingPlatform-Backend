// VideoProcessingPlatform.Core/Interfaces/IVideoProcessingService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for business logic related to video processing and transcoding management.
    public interface IVideoProcessingService
    {
        // Initiates a new transcoding job for an uploaded video using a specified encoding profile.
        Task<TranscodingJobInitiatedDto> InitiateTranscoding(Guid userId, InitiateTranscodingRequestDto request);

        // Retrieves the status of a specific transcoding job.
        Task<TranscodingJobDto?> GetTranscodingJobStatus(Guid jobId);

        // Retrieves all transcoding jobs for a given user.
        Task<IEnumerable<TranscodingJobDto>> GetUserTranscodingJobs(Guid userId);

        // Retrieves all transcoding jobs for a specific uploaded video.
        Task<IEnumerable<TranscodingJobDto>> GetTranscodingJobsForVideo(Guid videoId);

        // Placeholder for updating job progress from the worker (internal API/method)
        Task UpdateTranscodingJobProgress(Guid jobId, int progress, string statusMessage, string status);

        // Placeholder for completing a job from the worker (internal API/method)
        Task CompleteTranscodingJob(Guid jobId, List<VideoRenditionDto> renditions);

        // Placeholder for marking a job as failed (internal API/method)
        Task FailTranscodingJob(Guid jobId, string errorMessage);

        // Gets comprehensive video details including default thumbnail.
        Task<VideoDetailsDto?> GetVideoDetailsAsync(Guid videoId, Guid userId); // Assuming VideoDetailsDto includes thumbnail info

        // Gets all thumbnail options for a video.
        Task<IEnumerable<ThumbnailDto>> GetAllThumbnailsForVideoAsync(Guid videoId);

        // Sets a specific thumbnail as the default for a video.
        Task<SetSelectedThumbnailResponseDto> SetDefaultThumbnailAsync(Guid videoId, Guid thumbnailId);

        Task<bool> ThumbnailsExistForVideoAsync(Guid videoId);
    }
}
