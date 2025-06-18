// VideoProcessingPlatform.Core/DTOs/LoginRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; } // Or Email, depending on login strategy

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}