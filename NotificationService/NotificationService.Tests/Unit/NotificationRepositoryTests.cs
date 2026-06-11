using NotificationService.Core.Entities;
using NotificationService.Infrastructure.Services;
using NotificationService.Tests.Helpers;

namespace NotificationService.Tests.Unit;

public class NotificationRepositoryTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private static Notification New(Guid userId, string message = "m") => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Message = message,
        IsRead = false,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetByUserId_returns_only_that_users_notifications_newest_first()
    {
        var user = Guid.NewGuid();
        await using (var ctx = _factory.NewContext())
        {
            var repo = new NotificationRepository(ctx);
            await repo.CreateAsync(new Notification { Id = Guid.NewGuid(), UserId = user, Message = "old", CreatedAt = DateTime.UtcNow.AddMinutes(-5) });
            await repo.CreateAsync(new Notification { Id = Guid.NewGuid(), UserId = user, Message = "new", CreatedAt = DateTime.UtcNow });
            await repo.CreateAsync(New(Guid.NewGuid(), "other-user"));
        }

        await using var read = _factory.NewContext();
        var result = (await new NotificationRepository(read).GetByUserIdAsync(user)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("new", result[0].Message); // newest first
    }

    [Fact]
    public async Task MarkAsRead_sets_IsRead()
    {
        var id = Guid.NewGuid();
        await using (var ctx = _factory.NewContext())
        {
            await new NotificationRepository(ctx).CreateAsync(new Notification { Id = id, UserId = Guid.NewGuid(), Message = "m" });
        }

        await using (var upd = _factory.NewContext())
        {
            await new NotificationRepository(upd).MarkAsReadAsync(id);
        }

        await using var read = _factory.NewContext();
        Assert.True((await read.Notifications.FindAsync(id))!.IsRead);
    }

    public void Dispose() => _factory.Dispose();
}
