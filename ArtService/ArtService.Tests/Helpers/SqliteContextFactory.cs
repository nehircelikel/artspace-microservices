using ArtService.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArtService.Tests.Helpers;

/// <summary>Builds an ArtServiceContext on a fresh in-memory SQLite database.</summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public ArtServiceContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ArtServiceContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new ArtServiceContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
