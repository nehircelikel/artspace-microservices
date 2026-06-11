using CommentService.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Tests.Helpers;

/// <summary>
/// Builds a CommentServiceContext on a fresh in-memory SQLite database.
/// SQLite (unlike the EF InMemory provider) enforces relational features such as
/// ON DELETE CASCADE, which the parent/reply tests rely on. The connection is kept
/// open for the lifetime of the context so the :memory: database survives, and is
/// disposed when the context is disposed.
/// </summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public CommentServiceContext NewContext()
    {
        var options = new DbContextOptionsBuilder<CommentServiceContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new CommentServiceContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
