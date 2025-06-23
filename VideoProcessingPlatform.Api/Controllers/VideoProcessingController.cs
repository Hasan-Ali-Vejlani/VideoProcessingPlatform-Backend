// VideoProcessingPlatform.Api/Controllers/VideoProcessingController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims; // To get user ID from JWT token

namespace VideoProcessingPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Base route: /api/videoprocessing
    [Authorize(Policy = "AuthenticatedUser")] // All endpoints in this controller require authentication
    public class VideoProcessingController : ControllerBase
    {
        private readonly IVideoProcessingService _videoProcessingService;
        private readonly IEncodingProfileService _encodingProfileService; // To get active profiles for user selection

        public VideoProcessingController(
            IVideoProcessingService videoProcessingService,
            IEncodingProfileService encodingProfileService)
        {
            _videoProcessingService = videoProcessingService;
            _encodingProfileService = encodingProfileService;
        }

        // Helper to get current user's ID from JWT token claims
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                throw new UnauthorizedAccessException("User ID not found or invalid in token.");
            }
            return userId;
        }

        /// <summary>
        /// Gets all active encoding profiles available for users to select.
        /// </summary>
        /// <returns>A list of active encoding profiles.</returns>
        [HttpGet("encoding-profiles")] // GET /api/videoprocessing/encoding-profiles
        [AllowAnonymous] // Allow anyone to see available profiles if desired, or make it authenticated if necessary
        public async Task<ActionResult<IEnumerable<EncodingProfileDto>>> GetActiveEncodingProfiles()
        {
            try
            {
                var profiles = await _encodingProfileService.GetAllActiveEncodingProfiles();
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while retrieving active encoding profiles: {ex.Message}" });
            }
        }


        /// <summary>
        /// Initiates a transcoding job for an uploaded video.
        /// </summary>
        /// <param name="request">Details for the transcoding job (VideoId, EncodingProfileId).</param>
        /// <returns>Confirmation of job initiation.</returns>
        [HttpPost("transcode")] // POST /api/videoprocessing/transcode
        public async Task<IActionResult> InitiateTranscoding([FromBody] InitiateTranscodingRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var response = await _videoProcessingService.InitiateTranscoding(userId, request);
                return CreatedAtAction(nameof(GetTranscodingJobStatus), new { jobId = response.JobId }, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while initiating transcoding: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves the status of a specific transcoding job.
        /// </summary>
        /// <param name="jobId">The ID of the transcoding job.</param>
        /// <returns>Detailed transcoding job status.</returns>
        [HttpGet("transcoding-status/{jobId}")] // GET /api/videoprocessing/transcoding-status/{jobId}
        public async Task<ActionResult<TranscodingJobDto>> GetTranscodingJobStatus(Guid jobId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var jobStatus = await _videoProcessingService.GetTranscodingJobStatus(jobId);

                if (jobStatus == null || jobStatus.UserId != userId) // Ensure job belongs to the current user
                {
                    return NotFound(new { message = $"Transcoding job with ID '{jobId}' not found or unauthorized." });
                }
                return Ok(jobStatus);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves all transcoding jobs for the currently authenticated user.
        /// </summary>
        /// <returns>A list of transcoding jobs.</returns>
        [HttpGet("my-transcodes")] // GET /api/videoprocessing/my-transcodes
        public async Task<ActionResult<IEnumerable<TranscodingJobDto>>> GetUserTranscodingJobs()
        {
            try
            {
                var userId = GetCurrentUserId();
                var jobs = await _videoProcessingService.GetUserTranscodingJobs(userId);
                return Ok(jobs);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        // --- Internal/Worker-Facing Endpoints (could be in a separate internal API) ---
        // For this demo, placing them here but note they should be secured differently or on a separate port/internal service.
        // For production, these should ideally be secured by API Key or internal network rules, not just JWTBearer.

        /// <summary>
        /// INTERNAL: Endpoint for transcoding worker to update job progress.
        /// </summary>
        [HttpPut("internal/jobs/{jobId}/progress")]
        [AllowAnonymous] // TEMPORARY: In production, secure this with API Key or specific internal auth
        public async Task<IActionResult> UpdateJobProgress(Guid jobId, [FromBody] TranscodingJobDto update)
        {
            try
            {
                await _videoProcessingService.UpdateTranscodingJobProgress(jobId, update.Progress, update.StatusMessage ?? "", update.Status);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update job progress for {jobId}: {ex.Message}" });
            }
        }

        /// <summary>
        /// INTERNAL: Endpoint for transcoding worker to mark a job as complete and add renditions.
        /// </summary>
        [HttpPut("internal/jobs/{jobId}/complete")]
        [AllowAnonymous] // TEMPORARY: In production, secure this with API Key or specific internal auth
        public async Task<IActionResult> CompleteJob(Guid jobId, [FromBody] List<VideoRenditionDto> renditions)
        {
            try
            {
                await _videoProcessingService.CompleteTranscodingJob(jobId, renditions);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to complete job for {jobId}: {ex.Message}" });
            }
        }

        /// <summary>
        /// INTERNAL: Endpoint for transcoding worker to mark a job as failed.
        /// </summary>
        [HttpPut("internal/jobs/{jobId}/fail")]
        [AllowAnonymous] // TEMPORARY: In production, secure this with API Key or specific internal auth
        public async Task<IActionResult> FailJob(Guid jobId, [FromBody] string errorMessage)
        {
            try
            {
                await _videoProcessingService.FailTranscodingJob(jobId, errorMessage);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to fail job for {jobId}: {ex.Message}" });
            }
        }
    }
}
