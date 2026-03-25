using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure
{
    /// <summary>
    /// Creates the JWT Tokens that users need for authentication/authorization.
    /// </summary>
    /// <param name="jwtSettings"></param>
    public class TokenService (IOptions<JwtSettings> jwtSettings) : ITokenService
    {
        private readonly JwtSettings _jwtSettings = jwtSettings.Value;

        public string CreateToken(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrEmpty(user.Id.ToString()))
                throw new ArgumentException("User ID cannot be null or empty", nameof(user));

            if (string.IsNullOrEmpty(user.UserName))
                throw new ArgumentException("Username cannot be null or empty", nameof(user));

            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("Email cannot be null or empty", nameof(user));

            if (string.IsNullOrEmpty(user.FirstName))
                throw new ArgumentException("First Name cannot be null or empty", nameof(user));

            if (string.IsNullOrEmpty(user.LastName))
                throw new ArgumentException("Last Name cannot be null or empty", nameof(user));

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }
}
