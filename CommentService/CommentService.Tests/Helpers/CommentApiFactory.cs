using CommentService.Infrastructure.Data;
using CommentService.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommentService.Tests.Helpers;

/// <summary>
/// Hosts the real CommentService API in-process for HTTP integration tests, but
/// backed by an in-memory SQLite database and a no-op RabbitMQ publisher, so no
/// Postgres or RabbitMQ is required.
/// </summary>
public sealed class CommentApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CommentApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwt.Secret,
                ["Jwt:Issuer"] = TestJwt.Issuer,
                ["Jwt:Audience"] = TestJwt.Audience,
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Swap the Npgsql DbContext for our shared in-memory SQLite one.
            services.RemoveAll<DbContextOptions<CommentServiceContext>>();
            services.AddDbContext<CommentServiceContext>(o => o.UseSqlite(_connection));

            // Replace the real publisher (which opens a RabbitMQ connection) with a stub.
            services.RemoveAll<IRabbitMQPublisher>();
            services.AddSingleton<IRabbitMQPublisher, NoopPublisher>();

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<CommentServiceContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    private sealed class NoopPublisher : IRabbitMQPublisher
    {
        public void Publish(object message) { /* no-op for tests */ }
    }
}
