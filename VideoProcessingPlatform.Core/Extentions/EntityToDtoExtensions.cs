// VideoProcessingPlatform.Core/Extensions/EntityToDtoExtensions.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;
using System.Linq; // Required for LINQ extensions

namespace VideoProcessingPlatform.Core.Extensions
{
    // Static class for extension methods to map Entity Framework entities to DTOs.
    public static class EntityToDtoExtensions
    {
        public static ThumbnailDto ToDto(this Thumbnail thumbnail)
        {
            return new ThumbnailDto
            {
                Id = thumbnail.Id,
                UploadMetadataId = thumbnail.UploadMetadataId,
                StoragePath = thumbnail.StoragePath,
                TimestampSeconds = thumbnail.TimestampSeconds,
                Order = thumbnail.Order,
                IsDefault = thumbnail.IsDefault
            };
        }

        public static VideoRenditionDto ToDto(this VideoRendition rendition)
        {
            return new VideoRenditionDto
            {
                Id = rendition.Id,
                TranscodingJobId = rendition.TranscodingJobId,
                RenditionType = rendition.RenditionType,
                StoragePath = rendition.StoragePath,
                IsEncrypted = rendition.IsEncrypted,
                PlaybackUrl = rendition.PlaybackUrl,
                GeneratedAt = rendition.GeneratedAt,
                Resolution = rendition.Resolution,
                BitrateKbps = rendition.BitrateKbps
            };
        }

        // Add other ToDto extension methods here as needed for other entities
    }
}
