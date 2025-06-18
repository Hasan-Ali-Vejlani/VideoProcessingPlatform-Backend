// VideoProcessingPlatform.Api/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Interfaces; // For IAuthService
using Microsoft.AspNetCore.Authorization; // For [AllowAnonymous]

namespace VideoProcessingPlatform.Api.Controllers
{
    [ApiController] // Indicates this is an API controller
    [Route("api/[controller]")] // Sets the base route for this controller to /api/Auth
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // Constructor with dependency injection for IAuthService
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")] // Defines POST /api/Auth/register endpoint
        [AllowAnonymous] // Allows unauthenticated access for registration
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // Call the AuthService to handle registration logic
            var response = await _authService.RegisterUser(request);

            if (response.Token != null)
            {
                // Return 201 Created status for successful registration
                // Optionally, return the location of the newly created resource (though not strictly necessary for registration)
                return CreatedAtAction(nameof(Register), response);
            }
            else
            {
                // Handle specific error messages with appropriate HTTP status codes
                if (response.Message == "Username or Email already exists.")
                {
                    return Conflict(response); // 409 Conflict for duplicate entries
                }
                return BadRequest(response); // 400 Bad Request for other validation or general errors
            }
        }

        [HttpPost("login")] // Defines POST /api/Auth/login endpoint
        [AllowAnonymous] // Allows unauthenticated access for login
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // Call the AuthService to handle login logic
            var response = await _authService.LoginUser(request);

            if (response.Token != null)
            {
                return Ok(response); // 200 OK for successful login
            }
            else
            {
                return Unauthorized(response); // 401 Unauthorized for invalid credentials
            }
        }
    }
}