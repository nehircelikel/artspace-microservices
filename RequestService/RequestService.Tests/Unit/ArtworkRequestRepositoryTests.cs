using RequestService.Core.Entities;
using RequestService.Infrastructure.Services;
using RequestService.Tests.Helpers;

namespace RequestService.Tests.Unit;

public class ArtworkRequestRepositoryTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private static ArtworkRequest NewRequest(Guid artistId, Guid requesterId) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Commission",
        Description = "Paint my cat",
        Status = RequestStatus.Pending,
        ArtistId = artistId,
        ArtistUsername = "artist",
        RequesterId = requesterId,
        RequesterUsername = "client",
        RequesterEmail = "client@example.com",
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetReceived_and_GetSent_filter_by_owner()
    {
        var artist = Guid.NewGuid();
        var client = Guid.NewGuid();
        var other = Guid.NewGuid();

        await using (var ctx = _factory.NewContext())
        {
            ctx.ArtworkRequests.Add(NewRequest(artist, client));
            ctx.ArtworkRequests.Add(NewRequest(artist, other));
            ctx.ArtworkRequests.Add(NewRequest(other, client));
            await ctx.SaveChangesAsync();
        }

        await using var read = _factory.NewContext();
        var repo = new ArtworkRequestRepository(read);

        Assert.Equal(2, (await repo.GetReceivedAsync(artist)).Count());
        Assert.Equal(2, (await repo.GetSentAsync(client)).Count());
        Assert.Single(await repo.GetReceivedAsync(other));
    }

    [Fact]
    public async Task GetById_includes_logs_and_messages_in_order()
    {
        var id = Guid.NewGuid();
        await using (var ctx = _factory.NewContext())
        {
            var req = NewRequest(Guid.NewGuid(), Guid.NewGuid());
            req.Id = id;
            ctx.ArtworkRequests.Add(req);
            ctx.RequestLogs.Add(new RequestLog { Id = Guid.NewGuid(), RequestId = id, Action = "created", ActorUsername = "client", CreatedAt = DateTime.UtcNow });
            ctx.RequestMessages.Add(new RequestMessage { Id = Guid.NewGuid(), RequestId = id, SenderId = Guid.NewGuid(), SenderUsername = "client", Content = "hi", CreatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
        }

        await using var read = _factory.NewContext();
        var repo = new ArtworkRequestRepository(read);

        var loaded = await repo.GetByIdAsync(id);
        Assert.NotNull(loaded);
        Assert.Single(loaded!.Logs);
        Assert.Single(loaded.Messages);
    }

    [Fact]
    public async Task Deleting_request_cascades_to_logs_and_messages()
    {
        var id = Guid.NewGuid();
        await using (var ctx = _factory.NewContext())
        {
            var req = NewRequest(Guid.NewGuid(), Guid.NewGuid());
            req.Id = id;
            ctx.ArtworkRequests.Add(req);
            ctx.RequestLogs.Add(new RequestLog { Id = Guid.NewGuid(), RequestId = id, Action = "created", ActorUsername = "client" });
            ctx.RequestMessages.Add(new RequestMessage { Id = Guid.NewGuid(), RequestId = id, SenderId = Guid.NewGuid(), SenderUsername = "client", Content = "hi" });
            await ctx.SaveChangesAsync();
        }

        await using (var del = _factory.NewContext())
        {
            var req = await del.ArtworkRequests.FindAsync(id);
            del.ArtworkRequests.Remove(req!);
            await del.SaveChangesAsync();
        }

        await using var verify = _factory.NewContext();
        Assert.Empty(verify.ArtworkRequests);
        Assert.Empty(verify.RequestLogs);
        Assert.Empty(verify.RequestMessages);
    }

    public void Dispose() => _factory.Dispose();
}
