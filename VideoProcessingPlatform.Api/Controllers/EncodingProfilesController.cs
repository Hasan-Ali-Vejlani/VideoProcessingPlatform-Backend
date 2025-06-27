// VideoProcessingPlatform.Api/Controllers/EncodingProfilesController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic; // For IEnumerable

namespace VideoProcessingPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")] // Base route: /api/admin/encodingprofiles
    [Authorize(Roles = "Admin")] // Only users with 'Admin' role can access this controller
    public class EncodingProfilesController : ControllerBase
    {
        private readonly IEncodingProfileService _encodingProfileService;

        public EncodingProfilesController(IEncodingProfileService encodingProfileService)
        {
            _encodingProfileService = encodingProfileService;
        }

        [HttpGet] // GET /api/admin/encodingprofiles
        public async Task<ActionResult<IEnumerable<EncodingProfileDto>>> GetAllEncodingProfiles()
        {
            try
            {
                var profiles = await _encodingProfileService.GetAllEncodingProfiles();
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while retrieving encoding profiles: {ex.Message}" });
            }
        }

        [HttpGet("{id}")] // GET /api/admin/encodingprofiles/{id}
        public async Task<ActionResult<EncodingProfileDto>> GetEncodingProfileById(Guid id)
        {
            try
            {
                var profile = await _encodingProfileService.GetEncodingProfileById(id);
                if (profile == null)
                {
                    return NotFound(new { message = $"Encoding profile with ID '{id}' not found." });
                }
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while retrieving the encoding profile: {ex.Message}" });
            }
        }

        [HttpPost] // POST /api/admin/encodingprofiles
        public async Task<ActionResult<EncodingProfileDto>> CreateEncodingProfile([FromBody] CreateEncodingProfileDto request)
        {
            try
            {
                var createdProfile = await _encodingProfileService.CreateEncodingProfile(request);
                return CreatedAtAction(nameof(GetEncodingProfileById), new { id = createdProfile.Id }, createdProfile);
            }
            catch (ArgumentException ex) // For validation errors (e.g., missing fields, invalid template)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // For business logic errors (e.g., duplicate name)
            {
                return Conflict(new { message = ex.Message }); // 409 Conflict
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while creating the encoding profile: {ex.Message}" });
            }
        }

        [HttpPut("{id}")] // PUT /api/admin/encodingprofiles/{id}
        public async Task<ActionResult<EncodingProfileDto>> UpdateEncodingProfile(Guid id, [FromBody] UpdateEncodingProfileDto request)
        {
            try
            {
                var updatedProfile = await _encodingProfileService.UpdateEncodingProfile(id, request);
                return Ok(updatedProfile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while updating the encoding profile: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")] // DELETE /api/admin/encodingprofiles/{id}
        public async Task<IActionResult> SoftDeleteEncodingProfile(Guid id)
        {
            try
            {
                bool deleted = await _encodingProfileService.SoftDeleteEncodingProfile(id);
                if (!deleted) // Though service throws, a check here for safety
                {
                    return NotFound(new { message = $"Encoding profile with ID '{id}' not found or could not be soft-deleted." });
                }
                return NoContent(); // 204 No Content for successful deletion
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while soft deleting the encoding profile: {ex.Message}" });
            }
        }
    }
}
