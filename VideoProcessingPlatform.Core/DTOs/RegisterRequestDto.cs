// VideoProcessingPlatform.Core/DTOs/RegisterRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(3)] // Example validation
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MinLength(6)] // Example validation
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}