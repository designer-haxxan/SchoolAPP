using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SchoolManagementSystem.Data;

namespace SchoolManagementSystem.Services;

/// <summary>Issues signed JWT access tokens for the REST API. Settings live under the "Jwt" configuration section.</summary>
public class JwtTokenService(IConfiguration configuration)
{
    public const string Scheme = "Bearer";

    public string Issuer => configuration["Jwt:Issuer"] ?? "SchoolManagementSystem";
    public string Audience => configuration["Jwt:Audience"] ?? "SchoolManagementSystem.Api";

    public int ExpiryMinutes => int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) && m > 0 ? m : 120;

    /// <summary>Signing key must be at least 32 characters for HS256. Set it with user secrets or an environment variable.</summary>
    public SymmetricSecurityKey SigningKey
    {
        get
        {
            var key = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:Key is missing or shorter than 32 characters. Set it with user secrets or the Jwt__Key environment variable.");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }
    }

    public (string Token, DateTime ExpiresUtc) Create(ApplicationUser user, IEnumerable<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName ?? user.UserName ?? string.Empty),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(Issuer, Audience, claims, notBefore: DateTime.UtcNow, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
