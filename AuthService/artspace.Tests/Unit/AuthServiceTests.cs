using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using artspace.Infrastructure.Services;
using artspace.Tests.Helpers;

namespace artspace.Tests.Unit;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteContextFactory _db = new();

    private AuthService NewService() =>
        new(new UserRepository(_db.NewContext()), TestConfig.Build());

    [Fact]
    public async Task Register_hashes_password_and_persists_user()
    {
        var svc = NewService();
        var user = await svc.RegisterAsync("a@test.com", "secret123", "alice", "Visitor", null, null);

        Assert.NotEqual("secret123", user.PasswordHash);                 // hashed, not plaintext
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", user.PasswordHash));

        await using var verify = _db.NewContext();
        Assert.NotNull(await verify.Users.FindAsync(user.Id));
    }

    [Fact]
    public async Task Register_with_duplicate_email_throws()
    {
        var svc = NewService();
        await svc.RegisterAsync("dup@test.com", "pw", "u1", "Visitor", null, null);

        await Assert.ThrowsAsync<Exception>(() =>
            svc.RegisterAsync("dup@test.com", "pw2", "u2", "Visitor", null, null));
    }

    [Fact]
    public async Task Login_with_wrong_password_throws()
    {
        var svc = NewService();
        await svc.RegisterAsync("b@test.com", "right", "bob", "Visitor", null, null);

        await Assert.ThrowsAsync<Exception>(() => svc.LoginAsync("b@test.com", "wrong"));
    }

    [Fact]
    public async Task Login_returns_a_jwt_with_the_expected_claims()
    {
        var svc = NewService();
        var user = await svc.RegisterAsync("c@test.com", "pw", "carol", "Artist", null, null);

        var token = await svc.LoginAsync("c@test.com", "pw");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("carol", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Artist", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(TestConfig.Issuer, jwt.Issuer);
    }

    public void Dispose() => _db.Dispose();
}
