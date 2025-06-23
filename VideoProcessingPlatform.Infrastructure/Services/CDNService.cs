//// VideoProcessingPlatform.Infrastructure/Services/CDNService.cs
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using VideoProcessingPlatform.Core.Interfaces;
//// Add using directives for Azure CDN Management SDK if you plan to use it for Purge
//// For example: using Azure.ResourceManager.Cdn;

//namespace VideoProcessingPlatform.Infrastructure.Services
//{
//    // This is a placeholder CDN service. The actual implementation for Azure CDN
//    // signed URLs and cache invalidation will be added here.
//    public class CDNService : ICDNService
//    {
//        private readonly IConfiguration _configuration;
//        // Properties to hold CDN specific configuration values
//        private readonly string _cdnBaseUrl;
//        private readonly string _cdnSecurityKey; // This key would be configured in Azure CDN for token authentication

//        public CDNService(IConfiguration configuration)
//        {
//            _configuration = configuration;
//            // Retrieve CDN base URL and security key from appsettings.json
//            _cdnBaseUrl = _configuration["AzureCdn:BaseUrl"] ?? throw new InvalidOperationException("AzureCdn:BaseUrl not found in configuration.");
//            _cdnSecurityKey = _configuration["AzureCdn:SecurityKey"] ?? throw new InvalidOperationException("AzureCdn:SecurityKey not found in configuration. This key is used for CDN URL tokenization.");
//        }

//        public Task<string> GenerateSignedUrl(string storagePath, TimeSpan expiresIn)
//        {
//            // --- Placeholder Implementation for Signed URL Generation ---
//            // This needs to be replaced with actual Azure CDN token authentication logic.
//            // For Azure CDN Standard from Microsoft, you typically configure "Token authentication"
//            // on the CDN endpoint itself with a security key. The CDN then expects specific query
//            // parameters (like 'exp' for expiration, 'st' for start time, and a 'hash' parameter).
//            // The hash is derived from the URL components and the security key using a specific algorithm.
//            // This method would generate that hash and append the parameters to the CDN URL.

//            // For now, we'll return a dummy URL. The full implementation will come later.
//            var expiryTimestamp = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds();
//            string dummySignedUrl = $"{_cdnBaseUrl.TrimEnd('/')}/{storagePath.TrimStart('/')}?dummy_exp={expiryTimestamp}";
//            return Task.FromResult(dummySignedUrl);
//        }

//        public Task InvalidateCache(List<string> pathsToInvalidate)
//        {
//            // --- Placeholder Implementation for Cache Invalidation ---
//            // This would involve calling the Azure CDN Purge API, likely using Azure Management SDKs
//            // or direct REST API calls.
//            Console.WriteLine($"[CDNService] Simulating cache invalidation for paths: {string.Join(", ", pathsToInvalidate)}");
//            return Task.CompletedTask;
//        }
//    }
//}

// VideoProcessingPlatform.Infrastructure/Services/CDNService.cs
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography; // Required for HMACSHA256
using System.Text; // Required for Encoding
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.Interfaces;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Implementation of ICDNService for Azure CDN with URL token authentication.
    public class CDNService : ICDNService
    {
        private readonly IConfiguration _configuration;
        private readonly string _cdnBaseUrl;
        private readonly byte[] _cdnSecurityKeyBytes; // Stored as bytes for HMAC

        public CDNService(IConfiguration configuration)
        {
            _configuration = configuration;
            _cdnBaseUrl = _configuration["AzureCdn:BaseUrl"] ?? throw new InvalidOperationException("AzureCdn:BaseUrl not found in configuration.");

            // The SecurityKey from appsettings.json must be a Base64 string for Azure CDN Token Auth
            var securityKeyBase64 = _configuration["AzureCdn:SecurityKey"] ?? throw new InvalidOperationException("AzureCdn:SecurityKey not found in configuration. This key is used for CDN URL tokenization.");

            try
            {
                _cdnSecurityKeyBytes = Convert.FromBase64String(securityKeyBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("AzureCdn:SecurityKey is not a valid Base64 string. Ensure it's Base64 encoded as required by Azure CDN Token Authentication.", ex);
            }

            // Basic validation for CDN Base URL
            if (!Uri.TryCreate(_cdnBaseUrl, UriKind.Absolute, out Uri? cdnUri) || !(cdnUri.Scheme == "http" || cdnUri.Scheme == "https"))
            {
                throw new InvalidOperationException($"AzureCdn:BaseUrl '{_cdnBaseUrl}' is not a valid absolute HTTP/HTTPS URL.");
            }
        }

        /// <summary>
        /// Generates a signed URL for a given content path in Azure CDN using Token Authentication.
        /// This implementation follows the Azure CDN Standard from Microsoft token authentication specification.
        /// </summary>
        /// <param name="contentPath">The path to the content relative to the CDN origin (e.g., "renditions/jobId/file.mp4").</param>
        /// <param name="expiresIn">The duration for which the signed URL should be valid.</param>
        /// <returns>The signed URL as a string.</returns>
        public Task<string> GenerateSignedUrl(string contentPath, TimeSpan expiresIn)
        {
            if (string.IsNullOrWhiteSpace(contentPath))
            {
                throw new ArgumentException("Content path cannot be null or empty for signed URL generation.", nameof(contentPath));
            }
            if (expiresIn <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expiresIn), "Expiration time must be positive.");
            }

            // Calculate expiration timestamp (Unix seconds from epoch)
            // Azure CDN expects the 'exp' parameter in Unix epoch seconds (UTC).
            long expiryUnixSeconds = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds();

            // The string to sign (input for HMAC-SHA256) typically consists of:
            // 1. The relative path of the content (e.g., /renditions/jobId/file.mp4)
            // 2. The expiration timestamp (exp)
            // 3. Optional: Start time (st), Access Control (ac), Customer ID (id)
            // For a basic setup, we'll primarily use path and exp.
            // Azure CDN Token Authentication string for hashing:
            // For path based token auth: <Path><ExpirationParameterName>=<ExpirationValue>
            // E.g., /renditions/myvideo.mp4exp=1234567890
            // The path must start with a '/'
            string pathWithLeadingSlash = contentPath.StartsWith("/") ? contentPath : $"/{contentPath}";
            string stringToHash = $"{pathWithLeadingSlash}exp={expiryUnixSeconds}";

            // Generate the HMAC-SHA256 hash
            byte[] hashBytes;
            using (var hmac = new HMACSHA256(_cdnSecurityKeyBytes))
            {
                hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
            }

            // Convert hash to URL-safe Base64 string
            // Replace '+' with '-', '/' with '_', and remove trailing '='
            string base64Hash = Convert.ToBase64String(hashBytes)
                                    .Replace('+', '-')
                                    .Replace('/', '_')
                                    .TrimEnd('=');

            // Construct the final signed URL
            // Format: <CDN_BASE_URL>/<CONTENT_PATH>?exp=<EXPIRY>&h=<HASH>
            // Note: If contentPath already contains query parameters, you need to handle that carefully
            // by appending params with '&' instead of '?' for the first one.
            string signedUrl = $"{_cdnBaseUrl.TrimEnd('/')}/{contentPath}?exp={expiryUnixSeconds}&h={base64Hash}";

            return Task.FromResult(signedUrl);
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
            // Example using Azure Management SDK (requires package Azure.ResourceManager.Cdn):
            // var cdn = new ArmCdnClient(credential);
            // var cdnEndpoint = cdn.GetCdnEndpointResource(cdnEndpointId);
            // cdnEndpoint.PurgeContent(pathsToInvalidate);
            Console.WriteLine($"[CDNService] Simulating cache invalidation for paths: {string.Join(", ", pathsToInvalidate)}");
            return Task.CompletedTask;
        }
    }
}
