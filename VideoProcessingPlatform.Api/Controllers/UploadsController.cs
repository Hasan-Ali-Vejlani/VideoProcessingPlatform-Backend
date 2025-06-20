// VideoProcessingPlatform.Api/Controllers/UploadsController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims; // To get user ID from JWT token

namespace VideoProcessingPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Base route: /api/uploads
    [Authorize(Policy = "AuthenticatedUser")] // All endpoints in this controller require authentication
    public class UploadsController : ControllerBase
    {
        private readonly IUploadService _uploadService;

        public UploadsController(IUploadService uploadService)
        {
            _uploadService = uploadService;
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
        /// Initiates a new video upload session.
        /// </summary>
        /// <param name="request">Details of the file to be uploaded.</param>
        /// <returns>An Upload ID for the session and status.</returns>
        [HttpPost("initiate")] // POST /api/uploads/initiate
        public async Task<IActionResult> InitiateUpload([FromBody] InitiateUploadRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var response = await _uploadService.InitiateUpload(userId, request);
                return CreatedAtAction(nameof(InitiateUpload), new { id = response.UploadId }, response);
            }
            catch (Exception ex)
            {
                // Log the exception (using a proper logger like Serilog/NLog in a real app)
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Uploads a single chunk of a file.
        /// </summary>
        /// <param name="uploadId">The ID of the ongoing upload session.</param>
        /// <param name="chunkIndex">The 0-based index of the current chunk.</param>
        /// <param name="totalChunks">The total number of chunks for the file.</param>
        /// <param name="chunkData">The binary data of the chunk.</param>
        /// <returns>Status of the chunk upload and overall file completion.</returns>
        [HttpPost("chunk")] // POST /api/uploads/chunk
        // Use FromForm for multipart/form-data content type which is typical for file uploads
        public async Task<IActionResult> UploadChunk([FromForm] ChunkUploadRequestDto request)
        {
            try
            {
                // Ensure chunk data is present
                if (request.ChunkData == null || request.ChunkData.Length == 0)
                {
                    return BadRequest(new { message = "Chunk data is missing or empty." });
                }

                // Stream the chunk data
                using (var stream = request.ChunkData.OpenReadStream())
                {
                    var response = await _uploadService.ProcessChunk(
                        request.UploadId,
                        request.ChunkIndex,
                        request.TotalChunks,
                        stream
                    );
                    return Ok(response);
                }
            }
            catch (InvalidOperationException ex) // For business logic errors (e.g., upload not found)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex) // For chunk files not found during merge
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = $"An error occurred while processing chunk: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves the status of a specific upload session.
        /// </summary>
        /// <param name="uploadId">The ID of the upload session.</param>
        /// <returns>Detailed upload status.</returns>
        [HttpGet("{uploadId}/status")] // GET /api/uploads/{uploadId}/status
        public async Task<IActionResult> GetUploadStatus(Guid uploadId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var uploadStatus = await _uploadService.GetUploadStatus(uploadId);

                if (uploadStatus == null || uploadStatus.Id != uploadId) // Ensure it belongs to the current user
                {
                    return NotFound(new { message = $"Upload with ID {uploadId} not found or unauthorized." });
                }

                // A user can only see their own upload statuses
                // This check is implicitly handled by GetUploadStatus querying by userId,
                // but good to be explicit for security in controller.
                var userUploads = await _uploadService.GetUserUploads(userId, null); // Get all uploads for the user
                if (!userUploads.Any(u => u.Id == uploadId))
                {
                    return Forbid(); // 403 Forbidden if the upload does not belong to the user
                }


                return Ok(uploadStatus);
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
        /// Retrieves all upload statuses for the currently authenticated user.
        /// </summary>
        /// <param name="status">Optional: Filter by upload status (e.g., "InProgress", "Completed").</param>
        /// <returns>A list of upload statuses.</returns>
        [HttpGet] // GET /api/uploads
        public async Task<IActionResult> GetUserUploads([FromQuery] string? status = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var uploads = await _uploadService.GetUserUploads(userId, status);
                return Ok(uploads);
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
    }
}
