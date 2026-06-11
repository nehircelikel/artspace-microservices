using System.Text.Json;
using Moq;
using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Tests.Unit;

public class RequestNotificationTests
{
    [Fact]
    public async Task Valid_event_persists_the_message_verbatim()
    {
        var userId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new RequestNotificationEvent
        {
            UserId = userId,
            Message = "alice sent you an artwork request: Paint my cat",
        });

        var repo = new Mock<INotificationRepository>();
        Notification? saved = null;
        repo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => saved = n)
            .ReturnsAsync((Notification n) => n);

        await RequestNotificationConsumer.HandleMessageAsync(json, repo.Object);

        Assert.NotNull(saved);
        Assert.Equal(userId, saved!.UserId);
        Assert.Equal("alice sent you an artwork request: Paint my cat", saved.Message);
        Assert.False(saved.IsRead);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("null")]
    [InlineData("{\"userId\":\"00000000-0000-0000-0000-000000000000\",\"message\":\"x\"}")]
    [InlineData("{\"userId\":\"11111111-1111-1111-1111-111111111111\",\"message\":\"\"}")]
    public async Task Malformed_empty_or_targetless_messages_are_ignored(string message)
    {
        var repo = new Mock<INotificationRepository>();
        await RequestNotificationConsumer.HandleMessageAsync(message, repo.Object);
        repo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Never);
    }
}
