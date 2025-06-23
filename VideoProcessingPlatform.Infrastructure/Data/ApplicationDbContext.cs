// VideoProcessingPlatform.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using VideoProcessingPlatform.Core.Entities;
using System;
using BCrypt.Net;

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
        public DbSet<UploadMetadata> UploadMetadata { get; set; }
        public DbSet<EncodingProfile> EncodingProfiles { get; set; } // Add DbSet for EncodingProfile

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Configure UploadMetadata entity
            modelBuilder.Entity<UploadMetadata>(entity =>
            {
                entity.HasKey(um => um.Id);
                entity.Property(um => um.OriginalFileName).IsRequired().HasMaxLength(255);
                entity.Property(um => um.OriginalFileSize).IsRequired();
                entity.Property(um => um.MimeType).IsRequired().HasMaxLength(50);
                entity.Property(um => um.TotalChunks).IsRequired();
                entity.Property(um => um.CompletedChunks)
                      .IsRequired()
                      .HasColumnType("NVARCHAR(MAX)");
                entity.Property(um => um.OriginalStoragePath).HasMaxLength(512);
                entity.Property(um => um.UploadStatus).IsRequired().HasMaxLength(50);
                entity.Property(um => um.UploadedAt).IsRequired();

                entity.HasOne(um => um.User)
                      .WithMany()
                      .HasForeignKey(um => um.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure EncodingProfile entity
            modelBuilder.Entity<EncodingProfile>(entity =>
            {
                entity.HasKey(ep => ep.Id); // Primary Key
                entity.Property(ep => ep.ProfileName).IsRequired().HasMaxLength(255);
                entity.HasIndex(ep => ep.ProfileName).IsUnique(); // Unique constraint on ProfileName

                entity.Property(ep => ep.Description).HasMaxLength(1000); // Nullable description
                entity.Property(ep => ep.Resolution).IsRequired().HasMaxLength(50);
                entity.Property(ep => ep.BitrateKbps).IsRequired();
                entity.Property(ep => ep.Format).IsRequired().HasMaxLength(50);
                entity.Property(ep => ep.FFmpegArgsTemplate).IsRequired().HasColumnType("NVARCHAR(MAX)"); // Large text field
                entity.Property(ep => ep.IsActive).IsRequired();
                entity.Property(ep => ep.CreatedAt).IsRequired();
                entity.Property(ep => ep.LastModifiedAt).IsRequired();
            });


            // Seed an Admin user for initial setup
            string staticadminPasswordHash = "$2a$11$KUTK0Grj2NACm8nQRSuMQeMRXy9HN9M0Ab3bVj0.XDtgfVkGJEm/m";

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("C5761B01-7E04-4F58-8C64-46CE9EC3B24F"),
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = staticadminPasswordHash,
                    Role = "Admin",
                    CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
