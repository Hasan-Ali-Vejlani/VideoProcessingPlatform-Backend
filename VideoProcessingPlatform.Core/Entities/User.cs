// VideoProcessingPlatform.Core/Entities/User.cs
using System;
using System.Collections.Generic; // Required if you plan to add navigation properties later

namespace VideoProcessingPlatform.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Auto-generate GUID on creation
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } // Will store hashed password
        public string Role { get; set; } // e.g., "User", "Admin"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set creation timestamp
    }
}