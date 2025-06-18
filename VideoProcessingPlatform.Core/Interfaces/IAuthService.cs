// VideoProcessingPlatform.Core/Interfaces/IAuthService.cs
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs;

namespace VideoProcessingPlatform.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> RegisterUser(RegisterRequestDto request);
        Task<LoginResponseDto> LoginUser(LoginRequestDto request);
    }
}