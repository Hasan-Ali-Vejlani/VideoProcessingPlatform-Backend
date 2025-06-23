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
    }
}
