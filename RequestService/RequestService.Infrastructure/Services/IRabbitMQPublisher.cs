namespace RequestService.Infrastructure.Services;

public interface IRabbitMQPublisher
{
    // Notify a single user. The payload shape ({ UserId, Message }) is consumed by
    // NotificationService's RequestNotificationConsumer.
    void PublishNotification(Guid userId, string message);
}
