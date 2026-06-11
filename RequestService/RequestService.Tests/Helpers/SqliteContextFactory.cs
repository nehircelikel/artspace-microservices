using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RequestService.Infrastructure.Data;

namespace RequestService.Tests.Helpers;

/// <summary>
/// Builds a RequestServiceContext on a fresh in-memory SQLite database. SQLite (unlike
/// the EF InMemory provider) enforces relational features such as ON DELETE CASCADE,
/// which the request/log/message relationships rely on. The connection is kept open for
/// the lifetime of the factory so the :memory: database survives across contexts.
/// </summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public RequestServiceContext NewContext()
    {
        var options = new DbContextOptionsBuilder<RequestServiceContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new RequestServiceContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
