// VideoProcessingPlatform.Core/Entities/User.cs
using System;
using System.Collections.Generic; // Required for ICollection
using System.ComponentModel.DataAnnotations; // Required for attributes like [Required], [StringLength]

namespace VideoProcessingPlatform.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Auto-generate GUID on creation

        [Required]
        [StringLength(50)] // Consider max length for username
        public string Username { get; set; } = string.Empty; // Initialize to avoid nullable warnings

        [Required]
        [StringLength(255)] // Max length for email
        [EmailAddress] // Data annotation for email format validation
        public string Email { get; set; } = string.Empty; // Initialize to avoid nullable warnings

        [Required]
        [StringLength(255)] // Max length for hashed password
        public string PasswordHash { get; set; } = string.Empty; // Will store hashed password, initialize

        [Required]
        [StringLength(50)] // Max length for role
        public string Role { get; set; } = "User"; // e.g., "User", "Admin", initialize

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set creation timestamp

        // Collection of UploadMetadata records associated with this user
        public ICollection<UploadMetadata> UploadMetadata { get; set; } = new List<UploadMetadata>();

        // Collection of TranscodingJob records initiated by this user
        public ICollection<TranscodingJob> TranscodingJobs { get; set; } = new List<TranscodingJob>();
    }
}
