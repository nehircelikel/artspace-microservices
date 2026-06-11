using CommentService.Core.Entities;
using CommentService.Infrastructure.Services;
using CommentService.Tests.Helpers;

namespace CommentService.Tests.Unit;

public class CommentRepositoryTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private Comment NewReview(Guid artworkId, int rating, Guid? userId = null) => new()
    {
        Id = Guid.NewGuid(),
        Content = "review",
        Rating = rating,
        ArtworkId = artworkId,
        UserId = userId ?? Guid.NewGuid(),
        Username = "user",
        CreatedAt = DateTime.UtcNow,
    };

    private Comment NewReply(Guid artworkId, Guid parentId) => new()
    {
        Id = Guid.NewGuid(),
        Content = "reply",
        Rating = null,
        ArtworkId = artworkId,
        ParentId = parentId,
        UserId = Guid.NewGuid(),
        Username = "replier",
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetByArtworkId_returns_only_parents_with_nested_replies()
    {
        var artworkId = Guid.NewGuid();
        await using (var ctx = _factory.NewContext())
        {
            var review = NewReview(artworkId, 4);
            ctx.Comments.Add(review);
            ctx.Comments.Add(NewReply(artworkId, review.Id));
            ctx.Comments.Add(NewReply(artworkId, review.Id));
            await ctx.SaveChangesAsync();
        }

        await using var read = _factory.NewContext();
        var repo = new CommentRepository(read);

        var result = (await repo.GetByArtworkIdAsync(artworkId)).ToList();

        Assert.Single(result);                       // only the parent at top level
        Assert.Null(result[0].ParentId);
        Assert.Equal(2, result[0].Replies.Count);    // replies nested under it
    }

    [Fact]
    public async Task GetAverageRating_ignores_reply_null_ratings()
    {
        var artworkId = Guid.NewGuid();
        await using (var ctx = _factory.NewContext())
        {
            var r1 = NewReview(artworkId, 4);
            var r2 = NewReview(artworkId, 5);
            ctx.Comments.AddRange(r1, r2);
            ctx.Comments.Add(NewReply(artworkId, r1.Id)); // null rating must not count
            await ctx.SaveChangesAsync();
        }

        await using var read = _factory.NewContext();
        var repo = new CommentRepository(read);

        Assert.Equal(4.5, await repo.GetAverageRatingAsync(artworkId));
        Assert.Equal(2, await repo.GetRatingCountAsync(artworkId));
    }

    [Fact]
    public async Task GetAverageRating_is_zero_when_no_reviews()
    {
        await using var read = _factory.NewContext();
        var repo = new CommentRepository(read);
        Assert.Equal(0, await repo.GetAverageRatingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Deleting_parent_cascades_to_replies()
    {
        var artworkId = Guid.NewGuid();
        Guid parentId;
        await using (var ctx = _factory.NewContext())
        {
            var review = NewReview(artworkId, 3);
            parentId = review.Id;
            ctx.Comments.Add(review);
            ctx.Comments.Add(NewReply(artworkId, review.Id));
            await ctx.SaveChangesAsync();
        }

        await using (var del = _factory.NewContext())
        {
            await new CommentRepository(del).DeleteAsync(parentId);
        }

        await using var verify = _factory.NewContext();
        Assert.Empty(verify.Comments);   // both parent and reply gone (ON DELETE CASCADE)
    }

    [Fact]
    public async Task GetRatingsForArtworks_aggregates_per_artwork()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await using (var ctx = _factory.NewContext())
        {
            var ar = NewReview(a, 2);
            ctx.Comments.Add(ar);
            ctx.Comments.Add(NewReview(a, 4));
            ctx.Comments.Add(NewReply(a, ar.Id)); // ignored
            ctx.Comments.Add(NewReview(b, 5));
            await ctx.SaveChangesAsync();
        }

        await using var read = _factory.NewContext();
        var repo = new CommentRepository(read);

        var map = (await repo.GetRatingsForArtworksAsync(new[] { a, b }))
            .ToDictionary(r => r.ArtworkId);

        Assert.Equal(3.0, map[a].AverageRating);
        Assert.Equal(2, map[a].RatingCount);
        Assert.Equal(5.0, map[b].AverageRating);
        Assert.Equal(1, map[b].RatingCount);
    }

    public void Dispose() => _factory.Dispose();
}
