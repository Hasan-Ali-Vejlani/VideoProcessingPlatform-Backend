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
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<AzureCDNService> _logger;

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

                // First, ensure the CDN base URL has a trailing slash for correct path combining
                string cdnBase = _cdnBaseUrl.EndsWith("/") ? _cdnBaseUrl : _cdnBaseUrl + "/";

                // Extract the path and query from the blob SAS URL
                Uri blobUri = new Uri(blobSasUrl);
                string blobRelativePathAndQuery = blobUri.AbsolutePath + blobUri.Query;

                // Combine CDN base URL with the blob's path and SAS query parameters
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


        /// <param name="pathsToInvalidate">A list of content paths to invalidate in the CDN cache.</param>
        public Task InvalidateCache(List<string> pathsToInvalidate)
        {
            // This is a placeholder. A real implementation would interact with Azure CDN Purge API.
            _logger.LogInformation($"[CDNService] cache invalidation for paths: {string.Join(", ", pathsToInvalidate)}");
            return Task.CompletedTask;
        }
    }
}
