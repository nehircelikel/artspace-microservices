using System.Text.Json;
using Moq;
using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Tests.Unit;

public class HandleMessageTests
{
    [Fact]
    public async Task Review_event_creates_a_notification_for_the_artist()
    {
        var artistId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new CommentCreatedEvent
        {
            RecipientId = artistId, Username = "alice", Content = "great piece", IsReply = false,
        });

        var repo = new Mock<INotificationRepository>();
        Notification? saved = null;
        repo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => saved = n)
            .ReturnsAsync((Notification n) => n);

        await RabbitMQConsumer.HandleMessageAsync(json, repo.Object);

        Assert.NotNull(saved);
        Assert.Equal(artistId, saved!.UserId);
        Assert.Contains("alice", saved.Message);
        Assert.Contains("commented on your artwork", saved.Message);
        Assert.Contains("great piece", saved.Message);
        Assert.False(saved.IsRead);
    }

    [Fact]
    public async Task Reply_event_notifies_the_replied_to_user_with_a_reply_message()
    {
        var recipientId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new CommentCreatedEvent
        {
            RecipientId = recipientId, Username = "artist", Content = "thanks!", IsReply = true,
        });

        var repo = new Mock<INotificationRepository>();
        Notification? saved = null;
        repo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => saved = n)
            .ReturnsAsync((Notification n) => n);

        await RabbitMQConsumer.HandleMessageAsync(json, repo.Object);

        Assert.NotNull(saved);
        Assert.Equal(recipientId, saved!.UserId);
        Assert.Contains("replied to your comment", saved.Message);
    }

    [Fact]
    public async Task Event_with_empty_recipient_is_ignored()
    {
        var json = JsonSerializer.Serialize(new CommentCreatedEvent
        {
            RecipientId = Guid.Empty, Username = "alice", Content = "x",
        });

        var repo = new Mock<INotificationRepository>();
        await RabbitMQConsumer.HandleMessageAsync(json, repo.Object);
        repo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("null")]
    public async Task Malformed_or_null_message_is_ignored(string message)
    {
        var repo = new Mock<INotificationRepository>();
        await RabbitMQConsumer.HandleMessageAsync(message, repo.Object);
        repo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Never);
    }
}
