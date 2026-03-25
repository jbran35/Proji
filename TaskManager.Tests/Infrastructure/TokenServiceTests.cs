using FluentAssertions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure;

namespace TaskManager.Tests.Infrastructure
{
    public class TokenServiceTests
    {
        private readonly JwtSettings _jwtSettings;
        private readonly TokenService _tokenService;

        public TokenServiceTests()
        {
            _jwtSettings = new JwtSettings
            {
                Key = "SuperSecretKeyForTestingPurposeOnly!12345",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiresInMinutes = 60
            };

            var options = Options.Create(_jwtSettings);
            _tokenService = new TokenService(options);
        }

        [Fact]
        public void CreateToken_WithValidUser_ReturnsValidJwtToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var tokenString = _tokenService.CreateToken(user);

            // Assert
            tokenString.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
            jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
            
            // Validate claims
            var claims = jwtToken.Claims.ToList();
            claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
            claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
            claims.Should().Contain(c => c.Type == ClaimTypes.GivenName && c.Value == user.FirstName);
            claims.Should().Contain(c => c.Type == ClaimTypes.Surname && c.Value == user.LastName);
            claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        }

        [Fact]
        public void CreateToken_WithNullUser_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action action = () => _tokenService.CreateToken(null!);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CreateToken_WithEmptyUserName_ThrowsArgumentException()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "", // Invalid
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act & Assert
            Action action = () => _tokenService.CreateToken(user);
            action.Should().Throw<ArgumentException>().WithMessage("*Username cannot be null or empty*");
        }
    }
}