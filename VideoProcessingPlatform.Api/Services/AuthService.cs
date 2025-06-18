// VideoProcessingPlatform.Api/Services/AuthService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Entities;
using VideoProcessingPlatform.Core.Interfaces; // For IUserRepository and IJWTService
using System.Threading.Tasks;
using System;
using BCrypt.Net; // For password hashing and verification

namespace VideoProcessingPlatform.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJWTService _jwtService;

        public AuthService(IUserRepository userRepository, IJWTService jwtService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
        }

        public async Task<LoginResponseDto> RegisterUser(RegisterRequestDto request)
        {
            // 1. Check if user already exists by username or email to prevent duplicates
            var existingUser = await _userRepository.GetUserByUsernameOrEmail(request.Username, request.Email);
            if (existingUser != null)
            {
                // Return a meaningful error message if user already exists
                return new LoginResponseDto
                {
                    Message = "Username or Email already exists.",
                    Token = null // No token on registration failure
                };
            }

            // 2. Hash password securely using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Create a new User entity
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "User", // Default role for newly registered users
                CreatedAt = DateTime.UtcNow // Set creation timestamp
            };

            // 4. Add the new user to the database via the repository
            await _userRepository.Add(newUser);

            // 5. Generate a JWT token for immediate login upon successful registration
            var token = _jwtService.GenerateToken(newUser.Id, newUser.Role, DateTime.UtcNow.AddHours(1)); // Token valid for 1 hour

            // 6. Return success response with token and user details
            return new LoginResponseDto
            {
                Token = token,
                Message = "Registration successful!",
                Role = newUser.Role,
                UserId = newUser.Id
            };
        }

        public async Task<LoginResponseDto> LoginUser(LoginRequestDto request)
        {
            // 1. Retrieve user by username
            var user = await _userRepository.GetUserByUsername(request.Username);

            // 2. Verify user exists and password is correct using BCrypt
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Return error if credentials are invalid
                return new LoginResponseDto
                {
                    Message = "Invalid credentials.",
                    Token = null // No token on login failure
                };
            }

            // 3. Generate JWT token upon successful login
            var token = _jwtService.GenerateToken(user.Id, user.Role, DateTime.UtcNow.AddHours(1)); // Token valid for 1 hour

            // 4. Return success response with token and user details
            return new LoginResponseDto
            {
                Token = token,
                Message = "Login successful!",
                Role = user.Role,
                UserId = user.Id
            };
        }
    }
}