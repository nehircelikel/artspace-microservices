using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CommentService.Tests.Helpers;

/// <summary>
/// JWT settings shared by the test host and the token minter, mirroring the claim
/// scheme the services use in production (NameIdentifier = user id, Name = username).
/// </summary>
public static class TestJwt
{
    public const string Secret = "test-secret-key-for-integration-tests-only-1234567890";
    public const string Issuer = "artspace-test";
    public const string Audience = "artspace-test";

    public static string Create(Guid userId, string username, string role = "Visitor")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
