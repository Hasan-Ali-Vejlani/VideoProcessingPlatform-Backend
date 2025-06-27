// VideoProcessingPlatform.Infrastructure/Services/CDNService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // --- NEW: Added for ILogger ---
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.Interfaces;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Implementation of ICDNService for Azure CDN by generating Blob SAS URLs.
    public class AzureCDNService : ICDNService
    {
        private readonly IConfiguration _configuration;
        private readonly IFileStorageService _fileStorageService; // --- NEW: Inject IFileStorageService ---
        private readonly ILogger<AzureCDNService> _logger; // --- NEW: Logger instance ---

        private readonly string _cdnBaseUrl; // Your Azure Front Door/CDN endpoint base URL

        public AzureCDNService(IConfiguration configuration, IFileStorageService fileStorageService, ILogger<AzureCDNService> logger) // --- NEW: Inject dependencies ---
        {
            _configuration = configuration;
            _fileStorageService = fileStorageService; // Initialize
            _logger = logger; // Initialize

            // Retrieve CDN base URL from appsettings.json
            _cdnBaseUrl = _configuration["AzureCdn:BaseUrl"] ?? throw new InvalidOperationException("AzureCdn:BaseUrl not found in configuration.");

            if (!Uri.TryCreate(_cdnBaseUrl, UriKind.Absolute, out Uri? cdnUri) || !(cdnUri.Scheme == "http" || cdnUri.Scheme == "https"))
            {
                throw new InvalidOperationException($"AzureCdn:BaseUrl '{_cdnBaseUrl}' is not a valid absolute HTTP/HTTPS URL.");
            }
        }

        /// <summary>
        /// Generates a signed URL for a given content path.
        /// For this implementation, it generates an Azure Blob Storage SAS URL for the blob
        /// and then constructs a CDN-friendly URL using that SAS.
        /// </summary>
        /// <param name="storagePath">The full blob URI where the content is stored (e.g., from VideoRendition.StoragePath).</param>
        /// <param name="expiresIn">The duration for which the signed URL should be valid.</param>
        /// <returns>The CDN-backed SAS URL as a string.</returns>
        public async Task<string> GenerateSignedUrl(string storagePath, TimeSpan expiresIn)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                _logger.LogError("GenerateSignedUrl: storagePath cannot be null or empty.");
                throw new ArgumentException("Storage path cannot be null or empty for signed URL generation.", nameof(storagePath));
            }

            try
            {
                // 1. Generate the Blob SAS URL using AzureBlobStorageService
                string blobSasUrl = await _fileStorageService.GenerateBlobSasUrl(storagePath, expiresIn);
                _logger.LogInformation($"Generated Blob SAS URL for {storagePath}.");

                // 2. Construct the CDN-friendly URL by replacing the blob storage domain with the CDN domain.
                // Example:
                // Original Blob URI: https://yourstorageaccount.blob.core.windows.net/renditions/jobId/file.mp4
                // CDN Base URL:      https://videoplayback-abcdef.azurefd.net/
                // Target:            https://videoplayback-abcdef.azurefd.net/renditions/jobId/file.mp4?sv=... (SAS query params)

                // First, ensure the CDN base URL has a trailing slash for correct path combining
                string cdnBase = _cdnBaseUrl.EndsWith("/") ? _cdnBaseUrl : _cdnBaseUrl + "/";

                // Extract the path and query from the blob SAS URL
                Uri blobUri = new Uri(blobSasUrl);
                string blobRelativePathAndQuery = blobUri.AbsolutePath + blobUri.Query;

                // Combine CDN base URL with the blob's path and SAS query parameters
                // This assumes your Front Door is configured to access your Blob Storage at its root path,
                // and the container name (e.g., 'renditions') is part of the path in blobRelativePathAndQuery.
                // E.g., if blobUri.AbsolutePath is /renditions/jobId/file.mp4, this works directly.
                string finalCdnSignedUrl = $"{cdnBase.TrimEnd('/')}{blobRelativePathAndQuery}";

                _logger.LogInformation($"Constructed CDN-backed SAS URL: {finalCdnSignedUrl}");
                return finalCdnSignedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to generate CDN-backed signed URL for storage path: {storagePath}");
                throw;
            }
        }

        /// <summary>
        /// Simulates cache invalidation for Azure CDN.
        /// For a real implementation, this would use Azure Management SDKs or direct REST API calls
        /// to trigger a purge operation on the CDN endpoint.
        /// </summary>
        /// <param name="pathsToInvalidate">A list of content paths to invalidate in the CDN cache.</param>
        public Task InvalidateCache(List<string> pathsToInvalidate)
        {
            // This is a placeholder. A real implementation would interact with Azure CDN Purge API.
            _logger.LogInformation($"[CDNService] Simulating cache invalidation for paths: {string.Join(", ", pathsToInvalidate)}");
            return Task.CompletedTask;
        }
    }
}
