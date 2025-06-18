// VideoProcessingPlatform.Core/DTOs/LoginResponseDto.cs
using System;

namespace VideoProcessingPlatform.Core.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string Message { get; set; } // e.g., "Registration successful!"
        public string Role { get; set; }
        public Guid UserId { get; set; } // Optional, but useful for frontend
    }
}