using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VebTur.Application.Auth;
using VebTur.Application.Contracts.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace VebTur.Infrastructure.Auth;

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public JwtTokenResult GenerateToken(Guid userId, string email, IReadOnlyList<string> roles)
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(_options.ExpiryHours);

        // Claim types are deliberately the short JWT names (not System.Security.Claims.ClaimTypes'
        // long URIs) since JwtBearerOptions.MapInboundClaims defaults to false as of .NET 8 — claims
        // arrive on the server exactly as named here. Program.cs sets matching NameClaimType/RoleClaimType.
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            ..roles.Select(role => new Claim("role", role)),
        ];

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(tokenString, expiresAtUtc);
    }
}
