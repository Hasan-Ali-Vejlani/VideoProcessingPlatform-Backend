// VideoProcessingPlatform.Core/Interfaces/IUserRepository.cs
using System;
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.Entities;

namespace VideoProcessingPlatform.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsername(string username); // Use nullable reference type User?
        Task<User?> GetUserByUsernameOrEmail(string username, string email);
        Task<User> Add(User user);
        Task<bool> Update(User user);
    }
}