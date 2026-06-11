using ArtService.Core.Entities;
using ArtService.Infrastructure.Services;
using ArtService.Tests.Helpers;

namespace ArtService.Tests.Unit;

public class ArtworkRepositoryTests : IDisposable
{
    private readonly SqliteContextFactory _db = new();

    private static Artwork New(string title, string category, Guid? artistId = null, string description = "") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = description,
        ImageUrl = "http://img",
        Category = category,
        ArtistId = artistId ?? Guid.NewGuid(),
        ArtistUsername = "artist",
        CreatedAt = DateTime.UtcNow,
    };

    private async Task Seed(params Artwork[] artworks)
    {
        await using var ctx = _db.NewContext();
        ctx.Artworks.AddRange(artworks);
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Search_filters_by_category()
    {
        await Seed(New("A", "Painting"), New("B", "Sculpture"));
        await using var ctx = _db.NewContext();

        var result = await new ArtworkRepository(ctx).SearchAsync("Painting", null);

        Assert.Single(result);
        Assert.Equal("A", result.Single().Title);
    }

    [Fact]
    public async Task Search_filters_by_keyword_across_title_and_description()
    {
        await Seed(
            New("Sunset", "Painting", description: "warm colors"),
            New("Mountain", "Painting", description: "snowy peak"));
        await using var ctx = _db.NewContext();
        var repo = new ArtworkRepository(ctx);

        Assert.Single(await repo.SearchAsync(null, "Sunset"));   // title match
        Assert.Single(await repo.SearchAsync(null, "snowy"));    // description match
    }

    [Fact]
    public async Task Search_combines_category_and_keyword()
    {
        await Seed(
            New("Sunset", "Painting"),
            New("Sunset", "Photography"));
        await using var ctx = _db.NewContext();

        var result = await new ArtworkRepository(ctx).SearchAsync("Painting", "Sunset");

        Assert.Single(result);
        Assert.Equal("Painting", result.Single().Category);
    }

    [Fact]
    public async Task GetByArtistId_returns_only_that_artists_works()
    {
        var artist = Guid.NewGuid();
        await Seed(New("Mine", "Painting", artist), New("Theirs", "Painting"));
        await using var ctx = _db.NewContext();

        var result = await new ArtworkRepository(ctx).GetByArtistIdAsync(artist);

        Assert.Single(result);
        Assert.Equal("Mine", result.Single().Title);
    }

    [Fact]
    public async Task GetByArtistIdPaged_returns_page_slice_and_total()
    {
        var artist = Guid.NewGuid();
        var works = Enumerable.Range(0, 5)
            .Select(i => New($"W{i}", "Painting", artist))
            .ToArray();
        // Distinct timestamps so ordering (newest first) is deterministic.
        for (int i = 0; i < works.Length; i++) works[i].CreatedAt = DateTime.UtcNow.AddMinutes(i);
        await Seed(works);
        await Seed(New("Other", "Painting")); // different artist, must be excluded

        await using var ctx = _db.NewContext();
        var repo = new ArtworkRepository(ctx);

        var (page1, total) = await repo.GetByArtistIdPagedAsync(artist, page: 1, pageSize: 2);
        Assert.Equal(5, total);
        Assert.Equal(2, page1.Count());
        Assert.Equal("W4", page1.First().Title); // newest first

        var (page3, _) = await repo.GetByArtistIdPagedAsync(artist, page: 3, pageSize: 2);
        Assert.Single(page3); // 5 items, 2 per page → last page has 1
    }

    public void Dispose() => _db.Dispose();
}
