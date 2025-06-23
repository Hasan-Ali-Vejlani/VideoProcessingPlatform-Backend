// VideoProcessingPlatform.Core/Interfaces/ICDNService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Core.Interfaces
{
    public interface ICDNService
    {
        /// <summary>
        /// Generates a signed URL for a given storage path, valid for a specified duration.
        /// </summary>
        /// <param name="storagePath">The path of the content in storage (relative to CDN origin, e.g., "renditions/jobId/file.mp4").</param>
        /// <param name="expiresIn">The duration for which the signed URL should be valid.</param>
        /// <returns>The signed URL as a string.</returns>
        Task<string> GenerateSignedUrl(string storagePath, TimeSpan expiresIn);

        /// <summary>
        /// Invalidates the CDN cache for a list of specified paths.
        /// </summary>
        /// <param name="pathsToInvalidate">A list of content paths to invalidate in the CDN cache.</param>
        Task InvalidateCache(List<string> pathsToInvalidate);
    }
}
