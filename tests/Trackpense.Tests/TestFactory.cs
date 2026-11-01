using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trackpense.Core.Data;

namespace Trackpense.Tests;

/// <summary>
/// An <see cref="IDbContextFactory{T}"/> backed by a single in-memory SQLite connection that stays
/// open for the life of the test, so every context the services create shares the same database.
/// </summary>
public sealed class TestFactory : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TestFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
