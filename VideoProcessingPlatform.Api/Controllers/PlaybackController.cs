// VideoProcessingPlatform.Api/Controllers/PlaybackController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.Interfaces;
using VideoProcessingPlatform.Api.Extensions; // Required for GetUserId()

namespace VideoProcessingPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All playback endpoints require authentication
    public class PlaybackController : ControllerBase
    {
        private readonly IVideoPlaybackService _videoPlaybackService;

        public PlaybackController(IVideoPlaybackService videoPlaybackService)
        {
            _videoPlaybackService = videoPlaybackService;
        }

        [HttpGet("video/{videoId}/url")]
        public async Task<IActionResult> GetSignedVideoUrl(Guid videoId, [FromQuery] string renditionType = "default")
        {
            // Get the current user's ID from the JWT token
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized("User ID not found in token.");
            }

            var response = await _videoPlaybackService.GetSignedVideoUrl(videoId, renditionType, userId);

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                // Return appropriate HTTP status codes based on the service's message
                if (response.Message?.Contains("not found") == true)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }
        }
    }
}
