using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicSystem.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "DefaultSuperSecretKeyForDev123456!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("department", user.DepartmentId?.ToString() ?? "")
        };

        var expiresAt = DateTime.UtcNow.AddHours(
            double.Parse(_config["Jwt:ExpireHours"] ?? "12"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "ClinicSystem",
            audience: _config["Jwt:Audience"] ?? "ClinicSystem",
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
