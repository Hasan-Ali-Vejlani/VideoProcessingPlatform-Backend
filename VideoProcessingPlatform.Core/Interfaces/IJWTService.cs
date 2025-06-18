// VideoProcessingPlatform.Core/Interfaces/IJWTService.cs
using System;
using System.Threading.Tasks; // Not strictly needed for this one, but good to include for async methods

namespace VideoProcessingPlatform.Core.Interfaces
{
    public interface IJWTService
    {
        string GenerateToken(Guid userId, string role, DateTime expires);
    }
}