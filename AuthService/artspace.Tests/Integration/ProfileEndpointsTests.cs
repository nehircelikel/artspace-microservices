using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using artspace.API.DTOs;
using artspace.Tests.Helpers;

namespace artspace.Tests.Integration;

public class ProfileEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public ProfileEndpointsTests(AuthApiFactory factory) => _factory = factory;

    private static RegisterDto NewArtist(string email, string username) => new()
    {
        Email = email,
        Password = "pw123",
        Username = username,
        Role = "Artist",
        Bio = "I paint things.",
        ProfilePictureUrl = "http://img/pic.png",
    };

    [Fact]
    public async Task Register_persists_and_by_username_returns_public_profile()
    {
        var client = _factory.CreateClient();
        var username = $"artist_{Guid.NewGuid():N}";
        var reg = await client.PostAsJsonAsync("/api/Auth/register", NewArtist($"{username}@test.com", username));
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        var resp = await client.GetAsync($"/api/Auth/profile/by-username/{username}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var profile = await resp.Content.ReadFromJsonAsync<PublicProfileDto>();
        Assert.Equal(username, profile!.Username);
        Assert.Equal("Artist", profile.Role);
        Assert.Equal("I paint things.", profile.Bio);
        Assert.Equal("http://img/pic.png", profile.ProfilePictureUrl);
    }

    [Fact]
    public async Task Unknown_username_returns_404()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/Auth/profile/by-username/nobody_{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Updating_profile_requires_auth_and_persists()
    {
        var client = _factory.CreateClient();
        var username = $"artist_{Guid.NewGuid():N}";

        // Unauthenticated update is rejected.
        var anon = await client.PutAsJsonAsync("/api/Auth/profile", new UpdateProfileDto { Bio = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        // Register to obtain a token for this user.
        var regResp = await client.PostAsJsonAsync("/api/Auth/register", NewArtist($"{username}@test.com", username));
        var reg = await regResp.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reg!.Token);

        var update = await client.PutAsJsonAsync("/api/Auth/profile", new UpdateProfileDto
        {
            Bio = "Updated bio",
            ProfilePictureUrl = "http://img/new.png",
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        // Change is visible through the public lookup.
        var profile = await client.GetFromJsonAsync<PublicProfileDto>($"/api/Auth/profile/by-username/{username}");
        Assert.Equal("Updated bio", profile!.Bio);
        Assert.Equal("http://img/new.png", profile.ProfilePictureUrl);
    }
}
