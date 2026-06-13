using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Infrastructure.Services;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(
            queue: "comment_created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        _channel.QueueDeclare(
            queue: "commission_created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var commentEvent = JsonSerializer.Deserialize<CommentCreatedEvent>(message);

            if (commentEvent != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                await repo.CreateAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = commentEvent.ArtistId,
                    Message = $"{commentEvent.Username} commented on your artwork: {commentEvent.Content}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        };

        var commissionConsumer = new EventingBasicConsumer(_channel);
        commissionConsumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var commissionEvent = JsonSerializer.Deserialize<CommentCreatedEvent>(message);

            if (commissionEvent != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                await repo.CreateAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = commissionEvent.ArtistId,
                    Message = $"{commissionEvent.Username} {commissionEvent.Content}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        };

        _channel.BasicConsume(queue: "comment_created", autoAck: true, consumer: consumer);
        _channel.BasicConsume(queue: "commission_created", autoAck: true, consumer: commissionConsumer);
        return;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

public class CommentCreatedEvent
{
    public Guid ArtistId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}