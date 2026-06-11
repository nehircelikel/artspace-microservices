using System.Net;
using System.Net.Http.Json;
using artspace.API.DTOs;
using artspace.Tests.Helpers;

namespace artspace.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthEndpointsTests(AuthApiFactory factory) => _factory = factory;

    private static RegisterDto NewUser(string email) => new()
    {
        Email = email, Password = "pw123", Username = "user", Role = "Visitor",
    };

    [Fact]
    public async Task Register_then_login_then_profile_roundtrip()
    {
        var client = _factory.CreateClient();
        var email = $"u{Guid.NewGuid():N}@test.com";

        // Register → 200 + token + user
        var regResp = await client.PostAsJsonAsync("/api/Auth/register", NewUser(email));
        Assert.Equal(HttpStatusCode.OK, regResp.StatusCode);
        var reg = await regResp.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrEmpty(reg!.Token));
        Assert.Equal(email, reg.User.Email);

        // Duplicate register → 400
        var dup = await client.PostAsJsonAsync("/api/Auth/register", NewUser(email));
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);

        // Login valid → 200
        var login = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Email = email, Password = "pw123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Login invalid → 400
        var bad = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Email = email, Password = "nope" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        // Profile → 200
        var profile = await client.GetAsync($"/api/Auth/profile/{reg.User.Id}");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        // Unknown profile → 404
        var missing = await client.GetAsync($"/api/Auth/profile/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
