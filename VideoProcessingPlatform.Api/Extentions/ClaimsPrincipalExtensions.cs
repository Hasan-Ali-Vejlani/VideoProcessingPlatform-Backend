// VideoProcessingPlatform.Api/Extensions/ClaimsPrincipalExtensions.cs
using System;
using System.Security.Claims; // Required for ClaimsPrincipal and ClaimTypes

namespace VideoProcessingPlatform.Api.Extensions
{
    // Extension methods for ClaimsPrincipal to easily extract user information from JWT claims.
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Retrieves the user ID (Guid) from the ClaimsPrincipal.
        /// Assumes the user ID is stored in the ClaimTypes.NameIdentifier claim.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The user's Guid ID, or Guid.Empty if not found or invalid.</returns>
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            // Find the claim that stores the NameIdentifier (which we use for UserId)
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return userId;
            }
            return Guid.Empty; // Return empty GUID if claim not found or invalid
        }

        /// <summary>
        /// Retrieves the user's role from the ClaimsPrincipal.
        /// Assumes the role is stored in the ClaimTypes.Role claim.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
        /// <returns>The user's role string, or null if not found.</returns>
        public static string? GetUserRole(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}
