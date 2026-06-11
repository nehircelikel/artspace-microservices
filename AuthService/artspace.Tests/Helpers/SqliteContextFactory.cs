using artspace.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace artspace.Tests.Helpers;

/// <summary>Builds an ArtSpaceContext on a fresh in-memory SQLite database.</summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public ArtSpaceContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ArtSpaceContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new ArtSpaceContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
