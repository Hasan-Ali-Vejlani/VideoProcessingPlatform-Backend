// VideoProcessingPlatform.Infrastructure/Services/JWTService.cs
using Microsoft.Extensions.Configuration; // To read JWT settings from appsettings.json
using Microsoft.IdentityModel.Tokens; // Core JWT classes
using System;
using System.IdentityModel.Tokens.Jwt; // Handler for creating JWT
using System.Security.Claims; // For JWT claims
using System.Text; // For encoding JWT key
using VideoProcessingPlatform.Core.Interfaces;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    public class JWTService : IJWTService
    {
        private readonly IConfiguration _configuration;
        private readonly byte[] _key;

        public JWTService(IConfiguration configuration)
        {
            _configuration = configuration;
            // Retrieve the secret key from configuration. This is crucial for security.
            var secret = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(secret))
            {
                // Throwing an exception if key is missing is important for startup failure if misconfigured
                throw new InvalidOperationException("JWT:Key not found in configuration. Please add a strong, random key to appsettings.json.");
            }
            _key = Encoding.UTF8.GetBytes(secret);
        }

        public string GenerateToken(Guid userId, string role, DateTime expires)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // Define claims to be included in the JWT (e.g., user ID, role)
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            });

            // Describe the token to be created
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = expires, // Token expiration time
                // Signing credentials use the secret key to sign the token, verifying its integrity
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"], // Who issued the token
                Audience = _configuration["Jwt:Audience"] // Who the token is for
            };

            // Create and write the token
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}