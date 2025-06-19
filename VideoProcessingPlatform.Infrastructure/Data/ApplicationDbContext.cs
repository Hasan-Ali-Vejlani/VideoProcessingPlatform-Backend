// VideoProcessingPlatform.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using VideoProcessingPlatform.Core.Entities; // Reference your Core project's entities
using System;
using BCrypt.Net; // For Guid and DateTime

namespace VideoProcessingPlatform.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSet properties for each entity that maps to a database table
        public DbSet<User> Users { get; set; }
        // We will add other DbSets (UploadMetadata, EncodingProfile, etc.) as we implement those features.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraints as defined in ERD
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Seed an Admin user for initial setup
            // IMPORTANT: For production, you would retrieve sensitive data like passwords from secure configuration (e.g., Azure Key Vault).
            string staticadminPasswordHash = "$2a$11$KUTK0Grj2NACm8nQRSuMQeMRXy9HN9M0Ab3bVj0.XDtgfVkGJEm/m";

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("C5761B01-7E04-4F58-8C64-46CE9EC3B24F"), // A fixed GUID for predictable seeding
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = staticadminPasswordHash,
                    Role = "Admin", // Assign 'Admin' role
                    CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Set creation timestamp
                }
            );

            // Other entity configurations will go here as we add them
        }
    }
}