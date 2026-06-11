namespace CommentService.Infrastructure.Services;

public interface IRabbitMQPublisher
{
    void Publish(object message);
}
