// VideoProcessingPlatform.Api/Controllers/VideoProcessingController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims; // To get user ID from JWT token
using System.Linq; // Required for .Any() and .FirstOrDefault()

namespace VideoProcessingPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Base route: /api/videoprocessing
    [Authorize(Policy = "AuthenticatedUser")] // All endpoints in this controller require authentication
    public class VideoProcessingController : ControllerBase
    {
        private readonly IVideoProcessingService _videoProcessingService;
        private readonly IEncodingProfileService _encodingProfileService;

        public VideoProcessingController(
            IVideoProcessingService videoProcessingService,
            IEncodingProfileService encodingProfileService)
        {
            _videoProcessingService = videoProcessingService;
            _encodingProfileService = encodingProfileService;
        }

        // Helper to get current user's ID from JWT token claims (using the provided existing implementation)
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                throw new UnauthorizedAccessException("User ID not found or invalid in token.");
            }
            return userId;
        }

        [HttpGet("encoding-profiles")] // GET /api/videoprocessing/encoding-profiles
        [AllowAnonymous]
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

        [HttpPost("transcode")] // POST /api/videoprocessing/transcode
        public async Task<IActionResult> InitiateTranscoding([FromBody] InitiateTranscodingRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var response = await _videoProcessingService.InitiateTranscoding(userId, request);
                // Using CreatedAtAction to return a 201 Created status with a Location header
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

        [HttpGet("video/{videoId}/details")] // GET /api/videoprocessing/video/{videoId}/details
        public async Task<IActionResult> GetVideoDetails(Guid videoId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var videoDetails = await _videoProcessingService.GetVideoDetailsAsync(videoId, userId);

                if (videoDetails == null)
                {
                    // Return NotFound if video not found or user is not authorized to view it
                    return NotFound($"Video with ID '{videoId}' not found or you do not have access.");
                }
                return Ok(videoDetails);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while retrieving video details: {ex.Message}" });
            }
        }

        [HttpGet("video/{videoId}/thumbnails")] // GET /api/videoprocessing/video/{videoId}/thumbnails
        public async Task<IActionResult> GetAllThumbnailsForVideo(Guid videoId)
        {
            try
            {
                var userId = GetCurrentUserId();
                // First, check if the user has access to the video itself
                var videoExistsAndAuthorized = await _videoProcessingService.GetVideoDetailsAsync(videoId, userId) != null;
                if (!videoExistsAndAuthorized)
                {
                    return NotFound($"Video with ID '{videoId}' not found or you do not have access to its thumbnails.");
                }

                var thumbnails = await _videoProcessingService.GetAllThumbnailsForVideoAsync(videoId);
                return Ok(thumbnails);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while retrieving video thumbnails: {ex.Message}" });
            }
        }

        [HttpPut("video/{videoId}/thumbnail")] // PUT /api/videoprocessing/video/{videoId}/thumbnail
        public async Task<IActionResult> SetDefaultThumbnail(Guid videoId, [FromBody] SetSelectedThumbnailRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Basic validation: ensure videoId in URL matches request body
                if (request.VideoId != videoId)
                {
                    return BadRequest("Video ID in URL does not match Video ID in request body.");
                }

                // Verify the user owns/has access to this video and the thumbnail belongs to it
                var videoDetails = await _videoProcessingService.GetVideoDetailsAsync(videoId, userId);
                if (videoDetails == null)
                {
                    return NotFound($"Video with ID '{videoId}' not found or you do not have access to modify it.");
                }

                // Further validation: Ensure the selected thumbnail actually belongs to this video
                if (!videoDetails.AllThumbnails.Any(t => t.Id == request.ThumbnailId))
                {
                    return BadRequest($"Thumbnail with ID '{request.ThumbnailId}' does not belong to video '{videoId}'.");
                }

                var response = await _videoProcessingService.SetDefaultThumbnailAsync(videoId, request.ThumbnailId);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response); // Return BadRequest with the service's error message
                }
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while setting default thumbnail: {ex.Message}" });
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
        public async Task<IActionResult> UpdateJobProgress(Guid jobId, [FromBody] UpdateTranscodingProgressRequestDto update) // Changed from TranscodingJobDto to UpdateTranscodingProgressRequestDto
        {
            try
            {
                // Ensure the DTO passed to the service aligns with its parameters
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
