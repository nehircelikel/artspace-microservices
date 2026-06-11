using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ArtService.Tests.Helpers;

public static class TestJwt
{
    public const string Secret = "test-secret-key-for-integration-tests-only-1234567890";
    public const string Issuer = "artspace-test";
    public const string Audience = "artspace-test";

    public static readonly Dictionary<string, string?> Config = new()
    {
        ["Jwt:Secret"] = Secret,
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
    };

    public static string Create(Guid userId, string username, string role = "Artist")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
