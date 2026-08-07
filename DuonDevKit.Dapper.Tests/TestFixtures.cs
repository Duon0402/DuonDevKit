using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.Dapper.Tests
{
    public class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();
    }

    /// <summary>
    /// Backs a <see cref="TestDbContext"/> with a real SQLite database (in-memory, via a kept-open
    /// connection) instead of the EF Core InMemory provider, since InMemory treats transactions as a
    /// no-op and can't verify that Dapper calls actually participate in an ongoing EF Core transaction.
    /// </summary>
    public sealed class SqliteFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        public TestDbContext Context { get; }

        public SqliteFixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new TestDbContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
