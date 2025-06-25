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

        public DbSet<User> Users { get; set; } = default!;
        public DbSet<UploadMetadata> UploadMetadata { get; set; } = default!;
        public DbSet<EncodingProfile> EncodingProfiles { get; set; } = default!;
        public DbSet<TranscodingJob> TranscodingJobs { get; set; } = default!;
        public DbSet<VideoRendition> VideoRenditions { get; set; } = default!;
        public DbSet<Thumbnail> Thumbnails { get; set; } = default!; // --- NEW: DbSet for Thumbnail entity ---


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.Property(u => u.CreatedAt).IsRequired();

                entity.HasMany(u => u.UploadMetadata)
                      .WithOne(um => um.User)
                      .HasForeignKey(um => um.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.TranscodingJobs)
                      .WithOne(tj => tj.User)
                      .HasForeignKey(tj => tj.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure UploadMetadata entity
            modelBuilder.Entity<UploadMetadata>(entity =>
            {
                entity.HasKey(um => um.Id);
                entity.Property(um => um.OriginalFileName).IsRequired().HasMaxLength(255);
                entity.Property(um => um.OriginalFileSize).IsRequired();
                entity.Property(um => um.MimeType).IsRequired().HasMaxLength(100);
                entity.Property(um => um.TotalChunks).IsRequired();
                entity.Property(um => um.CompletedChunks)
                      .IsRequired()
                      .HasColumnType("NVARCHAR(MAX)");
                entity.Property(um => um.OriginalStoragePath).HasMaxLength(512);
                entity.Property(um => um.UploadStatus).IsRequired().HasMaxLength(50);
                entity.Property(um => um.UploadedAt).IsRequired();
                entity.Property(um => um.LastUpdatedAt).IsRequired();
                entity.Property(um => um.SelectedThumbnailUrl).HasMaxLength(512);

                entity.HasOne(um => um.User)
                      .WithMany(u => u.UploadMetadata)
                      .HasForeignKey(um => um.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(um => um.TranscodingJobs)
                      .WithOne(tj => tj.UploadMetadata)
                      .HasForeignKey(tj => tj.UploadMetadataId)
                      .OnDelete(DeleteBehavior.Restrict);

                // --- NEW: Relationship between UploadMetadata and Thumbnail ---
                entity.HasMany(um => um.Thumbnails) // As defined in UploadMetadata.cs
                      .WithOne(t => t.UploadMetadata)
                      .HasForeignKey(t => t.UploadMetadataId)
                      .OnDelete(DeleteBehavior.Cascade); // If video metadata is deleted, its thumbnails are deleted
            });

            // Configure EncodingProfile entity
            modelBuilder.Entity<EncodingProfile>(entity =>
            {
                entity.HasKey(ep => ep.Id);
                entity.Property(ep => ep.ProfileName).IsRequired().HasMaxLength(255);
                entity.HasIndex(ep => ep.ProfileName).IsUnique();
                entity.Property(ep => ep.Description).HasMaxLength(1000);
                entity.Property(ep => ep.Resolution).IsRequired().HasMaxLength(50);
                entity.Property(ep => ep.BitrateKbps).IsRequired();
                entity.Property(ep => ep.Format).IsRequired().HasMaxLength(50);
                entity.Property(ep => ep.FFmpegArgsTemplate).IsRequired().HasColumnType("NVARCHAR(MAX)");
                entity.Property(ep => ep.IsActive).IsRequired();
                entity.Property(ep => ep.CreatedAt).IsRequired();
                entity.Property(ep => ep.LastModifiedAt).IsRequired();
                entity.Property(ep => ep.ApplyDRM).IsRequired();

                entity.HasMany(ep => ep.TranscodingJobs)
                      .WithOne(tj => tj.EncodingProfile)
                      .HasForeignKey(tj => tj.EncodingProfileId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure TranscodingJob entity
            modelBuilder.Entity<TranscodingJob>(entity =>
            {
                entity.HasKey(tj => tj.Id);
                entity.Property(tj => tj.SourceStoragePath).IsRequired().HasMaxLength(512);
                entity.Property(tj => tj.Status).IsRequired().HasMaxLength(50);
                entity.Property(tj => tj.Progress).IsRequired();
                entity.Property(tj => tj.StatusMessage).HasMaxLength(1000);
                entity.Property(tj => tj.CreatedAt).IsRequired();
                entity.Property(tj => tj.LastUpdatedAt).IsRequired();
                entity.Property(tj => tj.EncodingProfileName).IsRequired().HasMaxLength(255);
                entity.Property(tj => tj.TargetResolution).IsRequired().HasMaxLength(50);
                entity.Property(tj => tj.TargetBitrateKbps).IsRequired();
                entity.Property(tj => tj.TargetFormat).IsRequired().HasMaxLength(50);
                entity.Property(tj => tj.FFmpegArgsTemplate).IsRequired().HasMaxLength(4000);
                entity.Property(tj => tj.ApplyDRM).IsRequired();

                entity.HasOne(tj => tj.UploadMetadata)
                      .WithMany(um => um.TranscodingJobs)
                      .HasForeignKey(tj => tj.UploadMetadataId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(tj => tj.User)
                      .WithMany(u => u.TranscodingJobs)
                      .HasForeignKey(tj => tj.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(tj => tj.EncodingProfile)
                      .WithMany(ep => ep.TranscodingJobs)
                      .HasForeignKey(tj => tj.EncodingProfileId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(tj => tj.VideoRenditions)
                      .WithOne(vr => vr.TranscodingJob)
                      .HasForeignKey(vr => vr.TranscodingJobId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure VideoRendition entity
            modelBuilder.Entity<VideoRendition>(entity =>
            {
                entity.HasKey(vr => vr.Id);
                entity.Property(vr => vr.RenditionType).IsRequired().HasMaxLength(100);
                entity.Property(vr => vr.StoragePath).IsRequired().HasMaxLength(512);
                entity.Property(vr => vr.IsEncrypted).IsRequired();
                entity.Property(vr => vr.PlaybackUrl).HasMaxLength(1024);
                entity.Property(vr => vr.GeneratedAt).IsRequired();
                entity.Property(vr => vr.Resolution).IsRequired().HasMaxLength(50);
                entity.Property(vr => vr.BitrateKbps).IsRequired();
            });

            // --- NEW: Configure Thumbnail entity ---
            modelBuilder.Entity<Thumbnail>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.StoragePath).IsRequired().HasMaxLength(512); // Path to the thumbnail image
                entity.Property(t => t.TimestampSeconds).IsRequired(); // Time in video (seconds)
                entity.Property(t => t.Order).IsRequired(); // Display order
                entity.Property(t => t.IsDefault).IsRequired(); // Whether it's the default
                entity.Property(t => t.GeneratedAt).IsRequired();

                entity.HasOne(t => t.UploadMetadata) // One-to-many relationship with UploadMetadata
                      .WithMany(um => um.Thumbnails) // Navigation property on UploadMetadata
                      .HasForeignKey(t => t.UploadMetadataId)
                      .OnDelete(DeleteBehavior.Cascade); // If UploadMetadata is deleted, its thumbnails are deleted
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
