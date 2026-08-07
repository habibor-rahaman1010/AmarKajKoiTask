using AmarKajKoi.Entities;
using AmarKajKoi.ServicesInterface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AmarKajKoi.ServicesImplement
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;
        public JwtTokenService(IConfiguration config) { _config = config; }

        public string GenerateToken(User user)
        {
            var section = _config.GetSection("Jwt");
            var secret = section["Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");
            var issuer = section["Issuer"] ?? "AmarKajKoi";
            var audience = section["Audience"] ?? "AmarKajKoiUsers";
            var expiresMinutes = int.TryParse(section["ExpiresMinutes"], out var m) ? m : 480;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("fullName", user.FullName),
                new Claim(ClaimTypes.Role, user.RoleName ?? string.Empty),
                new Claim("roleId", user.RoleId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
