using RequestService.API.DTOs;
using RequestService.Tests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RequestService.Tests.Integration;

public class RequestEndpointsTests : IClassFixture<RequestApiFactory>
{
    private readonly RequestApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public RequestEndpointsTests(RequestApiFactory factory) => _factory = factory;

    private HttpClient ClientFor(Guid userId, string username, string email = "u@example.com")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(userId, username, email));
        return client;
    }

    [Fact]
    public async Task Creating_a_request_requires_authentication()
    {
        var anon = _factory.CreateClient();
        var res = await anon.PostAsJsonAsync("/api/Request", new CreateRequestDto { Title = "X", ArtistId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Full_lifecycle_create_estimate_accept_and_message()
    {
        var artistId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var artist = ClientFor(artistId, "artist", "artist@example.com");
        var client = ClientFor(clientId, "client", "client@example.com");

        // Client creates a request.
        var createRes = await client.PostAsJsonAsync("/api/Request", new CreateRequestDto
        {
            Title = "Paint my cat",
            Description = "A portrait of Mittens",
            Budget = 150m,
            ArtistId = artistId,
            ArtistUsername = "artist",
        });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<RequestResponseDto>(Json);
        Assert.NotNull(created);
        Assert.Equal("Pending", created!.Status);
        Assert.Equal("client@example.com", created.RequesterEmail);
        var id = created.Id;

        // Shows up in both lists.
        var sent = await client.GetFromJsonAsync<List<RequestResponseDto>>("/api/Request/sent", Json);
        var received = await artist.GetFromJsonAsync<List<RequestResponseDto>>("/api/Request/received", Json);
        Assert.Contains(sent!, r => r.Id == id);
        Assert.Contains(received!, r => r.Id == id);

        // Artist cannot accept without estimates.
        var early = await artist.PostAsync($"/api/Request/{id}/accept", null);
        Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);

        // Artist sets estimates.
        var put = await artist.PutAsJsonAsync($"/api/Request/{id}", new UpdateRequestDto
        {
            EstimatedCost = 200m,
            EstimatedTime = "2 weeks",
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        // Now accept succeeds.
        var accept = await artist.PostAsync($"/api/Request/{id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var accepted = await accept.Content.ReadFromJsonAsync<RequestResponseDto>(Json);
        Assert.Equal("Accepted", accepted!.Status);

        // Both participants can message; a stranger cannot read the request.
        await client.PostAsJsonAsync($"/api/Request/{id}/messages", new CreateMessageDto { Content = "Thank you!" });
        await artist.PostAsJsonAsync($"/api/Request/{id}/messages", new CreateMessageDto { Content = "On it." });

        var detail = await artist.GetFromJsonAsync<RequestResponseDto>($"/api/Request/{id}", Json);
        Assert.Equal(2, detail!.Messages.Count);
        Assert.NotEmpty(detail.Logs); // creation + estimate edit + accept

        var stranger = ClientFor(Guid.NewGuid(), "nosy");
        var forbidden = await stranger.GetAsync($"/api/Request/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Requester_can_withdraw_a_pending_request()
    {
        var artistId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var artist = ClientFor(artistId, "artist");
        var client = ClientFor(clientId, "client");

        var createRes = await client.PostAsJsonAsync("/api/Request", new CreateRequestDto
        {
            Title = "Sketch", ArtistId = artistId, ArtistUsername = "artist",
        });
        var created = await createRes.Content.ReadFromJsonAsync<RequestResponseDto>(Json);

        // Artist cannot withdraw; requester can.
        var artistTry = await artist.PostAsync($"/api/Request/{created!.Id}/withdraw", null);
        Assert.Equal(HttpStatusCode.Forbidden, artistTry.StatusCode);

        var withdraw = await client.PostAsync($"/api/Request/{created.Id}/withdraw", null);
        Assert.Equal(HttpStatusCode.OK, withdraw.StatusCode);
        var result = await withdraw.Content.ReadFromJsonAsync<RequestResponseDto>(Json);
        Assert.Equal("Withdrawn", result!.Status);
    }
}
