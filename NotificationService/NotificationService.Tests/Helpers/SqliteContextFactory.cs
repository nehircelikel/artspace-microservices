using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Tests.Helpers;

/// <summary>Builds a NotificationContext on a fresh in-memory SQLite database.</summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public NotificationContext NewContext()
    {
        var options = new DbContextOptionsBuilder<NotificationContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new NotificationContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
