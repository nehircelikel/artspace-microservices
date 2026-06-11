using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArtService.API.DTOs;
using ArtService.Tests.Helpers;

namespace ArtService.Tests.Integration;

public class ArtworkEndpointsTests : IClassFixture<ArtApiFactory>
{
    private readonly ArtApiFactory _factory;

    public ArtworkEndpointsTests(ArtApiFactory factory) => _factory = factory;

    private HttpClient ClientFor(Guid userId, string username = "artist")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(userId, username));
        return client;
    }

    [Fact]
    public async Task Creating_without_a_token_is_unauthorized()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/Artwork", new CreateArtworkDto { Title = "T", Category = "Painting" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Create_then_get_roundtrip()
    {
        var client = ClientFor(Guid.NewGuid(), "creator");
        var createResp = await client.PostAsJsonAsync("/api/Artwork",
            new CreateArtworkDto { Title = "My Piece", Category = "Painting", ImageUrl = "http://img" });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<ArtworkResponseDto>();

        var getResp = await client.GetFromJsonAsync<ArtworkResponseDto>($"/api/Artwork/{created!.Id}");
        Assert.Equal("My Piece", getResp!.Title);
        Assert.Equal("creator", getResp.ArtistUsername);
    }

    [Fact]
    public async Task Artist_paged_endpoint_returns_envelope_with_totals()
    {
        var artistId = Guid.NewGuid();
        var client = ClientFor(artistId, "galleryowner");
        for (int i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/Artwork",
                new CreateArtworkDto { Title = $"P{i}", Category = "Painting", ImageUrl = "http://img" });
        }

        var paged = await client.GetFromJsonAsync<PagedResult<ArtworkResponseDto>>(
            $"/api/Artwork/artist/{artistId}/paged?page=1&pageSize=2");

        Assert.Equal(3, paged!.Total);
        Assert.Equal(2, paged.Items.Count());
        Assert.Equal(2, paged.TotalPages);
        Assert.Equal(1, paged.Page);
    }

    [Fact]
    public async Task Updating_someone_elses_artwork_is_forbidden()
    {
        var owner = ClientFor(Guid.NewGuid(), "owner");
        var created = await (await owner.PostAsJsonAsync("/api/Artwork",
            new CreateArtworkDto { Title = "Owned", Category = "Painting", ImageUrl = "http://img" }))
            .Content.ReadFromJsonAsync<ArtworkResponseDto>();

        var intruder = ClientFor(Guid.NewGuid(), "intruder");
        var resp = await intruder.PutAsJsonAsync($"/api/Artwork/{created!.Id}",
            new UpdateArtworkDto { Title = "Hijacked" });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
