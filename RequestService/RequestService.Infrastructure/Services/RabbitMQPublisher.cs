using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace RequestService.Infrastructure.Services;

public class RabbitMQPublisher : IRabbitMQPublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public const string QueueName = "request_notification";

    public RabbitMQPublisher()
    {
        var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
        var factory = new ConnectionFactory { HostName = hostName };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public void PublishNotification(Guid userId, string message)
    {
        var json = JsonSerializer.Serialize(new { UserId = userId, Message = message });
        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(
            exchange: "",
            routingKey: QueueName,
            basicProperties: null,
            body: body);
    }
}
