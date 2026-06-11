using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommentService.API.DTOs;
using CommentService.Tests.Helpers;

namespace CommentService.Tests.Integration;

public class CommentEndpointsTests : IClassFixture<CommentApiFactory>
{
    private readonly CommentApiFactory _factory;

    public CommentEndpointsTests(CommentApiFactory factory) => _factory = factory;

    private HttpClient ClientFor(Guid userId, string username, string role = "Visitor")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(userId, username, role));
        return client;
    }

    [Fact]
    public async Task Full_review_and_reply_flow()
    {
        var artistId = Guid.NewGuid();
        var visitorId = Guid.NewGuid();
        var artworkId = Guid.NewGuid();

        // Visitor posts a review with a rating.
        var visitor = ClientFor(visitorId, "visitor");
        var reviewResp = await visitor.PostAsJsonAsync("/api/Comment", new CreateCommentDto
        {
            Content = "Nice work", Rating = 4, ArtworkId = artworkId, ArtistId = artistId,
        });
        Assert.Equal(HttpStatusCode.Created, reviewResp.StatusCode);
        var review = await reviewResp.Content.ReadFromJsonAsync<CommentResponseDto>();
        Assert.Equal(4, review!.Rating);

        // Artist cannot review their own artwork.
        var artist = ClientFor(artistId, "artist", "Artist");
        var selfReview = await artist.PostAsJsonAsync("/api/Comment", new CreateCommentDto
        {
            Content = "self", Rating = 5, ArtworkId = artworkId, ArtistId = artistId,
        });
        Assert.Equal(HttpStatusCode.Forbidden, selfReview.StatusCode);

        // Artist CAN reply (no rating) — the regression we used to check by curl.
        var replyResp = await artist.PostAsJsonAsync("/api/Comment", new CreateCommentDto
        {
            Content = "Thanks!", ArtworkId = artworkId, ArtistId = artistId, ParentId = review.Id,
        });
        Assert.Equal(HttpStatusCode.Created, replyResp.StatusCode);
        var reply = await replyResp.Content.ReadFromJsonAsync<CommentResponseDto>();
        Assert.Null(reply!.Rating);

        // Threaded fetch: one parent with the nested reply.
        var threaded = await visitor.GetFromJsonAsync<List<CommentResponseDto>>($"/api/Comment/artwork/{artworkId}");
        Assert.Single(threaded!);
        Assert.Single(threaded![0].Replies);

        // Overall rating ignores the reply's null rating.
        var rating = await visitor.GetFromJsonAsync<ArtworkRatingDto>($"/api/Comment/artwork/{artworkId}/rating");
        Assert.Equal(4, rating!.AverageRating);
        Assert.Equal(1, rating.RatingCount);

        // Batch ratings endpoint.
        var batch = await visitor.GetFromJsonAsync<List<BatchArtworkRatingDto>>($"/api/Comment/ratings?artworkIds={artworkId}");
        Assert.Single(batch!);
        Assert.Equal(artworkId, batch![0].ArtworkId);
        Assert.Equal(4, batch[0].AverageRating);
    }

    [Fact]
    public async Task A_user_can_only_review_an_artwork_once()
    {
        var visitorId = Guid.NewGuid();
        var artworkId = Guid.NewGuid();
        var visitor = ClientFor(visitorId, "visitor");

        var first = await visitor.PostAsJsonAsync("/api/Comment", new CreateCommentDto
        {
            Content = "first", Rating = 4, ArtworkId = artworkId, ArtistId = Guid.NewGuid(),
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await visitor.PostAsJsonAsync("/api/Comment", new CreateCommentDto
        {
            Content = "again", Rating = 2, ArtworkId = artworkId, ArtistId = Guid.NewGuid(),
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Posting_a_comment_without_a_token_is_unauthorized()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/Comment", new CreateCommentDto
        {
            Content = "x", Rating = 3, ArtworkId = Guid.NewGuid(), ArtistId = Guid.NewGuid(),
        });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
