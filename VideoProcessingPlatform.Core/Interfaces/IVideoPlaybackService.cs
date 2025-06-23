// VideoProcessingPlatform.Core/Interfaces/IVideoPlaybackService.cs
using System;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs; // For SignedUrlDto

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for managing video playback related operations,
    // primarily focusing on generating secure playback URLs.
    public interface IVideoPlaybackService
    {
        /// <summary>
        /// Generates a signed URL for a specific video rendition, ensuring authorized access.
        /// </summary>
        /// <param name="videoId">The ID of the original video (UploadMetadataId).</param>
        /// <param name="requestedRenditionType">The preferred rendition type (e.g., "HLS_720p").</param>
        /// <param name="userId">The ID of the user requesting playback (for authorization checks).</param>
        /// <returns>A SignedUrlDto containing the URL if successful, and a message.</returns>
        Task<SignedUrlDto> GetSignedVideoUrl(Guid videoId, string requestedRenditionType, Guid userId);

        // You might add other playback-related methods here in the future, such as:
        // Task<IEnumerable<VideoRenditionDto>> GetAvailableRenditions(Guid videoId);
        // Task<bool> RecordPlaybackEvent(Guid userId, Guid videoId, string renditionType, TimeSpan duration);
    }
}
