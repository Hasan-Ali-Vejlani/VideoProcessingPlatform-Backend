// VideoProcessingPlatform.Core/DTOs/ThumbnailDTOs.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace VideoProcessingPlatform.Core.DTOs
{
    // DTO for representing a single thumbnail image.
    public class ThumbnailDto
    {
        public Guid Id { get; set; }
        public Guid UploadMetadataId { get; set; } // The video this thumbnail belongs to
        public string StoragePath { get; set; } = string.Empty; // The URL to the thumbnail image
        public int TimestampSeconds { get; set; } // Timestamp in video where thumbnail was captured
        public int Order { get; set; } // Order for display
        public bool IsDefault { get; set; } // Indicates if this is the currently selected default thumbnail
        public string? SignedUrl { get; set; }
    }

    // DTO for requesting to set a specific thumbnail as the default for a video.
    public class SetSelectedThumbnailRequestDto
    {
        [Required(ErrorMessage = "Video ID is required.")]
        public Guid VideoId { get; set; } // The ID of the UploadMetadata (video)

        [Required(ErrorMessage = "Thumbnail ID is required.")]
        public Guid ThumbnailId { get; set; } // The ID of the specific thumbnail to set as default
    }

    // DTO for the response after setting a default thumbnail.
    public class SetSelectedThumbnailResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? NewSelectedThumbnailUrl { get; set; } // The URL of the newly selected thumbnail
    }
}
