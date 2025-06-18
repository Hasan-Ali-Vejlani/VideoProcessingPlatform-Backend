// VideoProcessingPlatform.Infrastructure/Repositories/UserRepository.cs
using Microsoft.EntityFrameworkCore;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces;
using VideoProcessingPlatform.Infrastructure.Data;
using System.Threading.Tasks;
using System;

namespace VideoProcessingPlatform.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByUsernameOrEmail(string username, string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
        }

        public async Task<User> Add(User user)
        {
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync(); // Persist changes to database
            return user;
        }

        public async Task<bool> Update(User user)
        {
            _dbContext.Users.Update(user);
            return await _dbContext.SaveChangesAsync() > 0; // Returns true if changes were saved
        }
    }
}