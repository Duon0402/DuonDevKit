using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.Dapper.Tests
{
    public class DapperQueriesTests
    {
        [Fact]
        public async Task QueryAsync_MatchingRows_ReturnsThem()
        {
            using var fixture = new SqliteFixture();
            fixture.Context.Widgets.AddRange(new Widget { Name = "A" }, new Widget { Name = "B" });
            await fixture.Context.SaveChangesAsync();
            var queries = new DapperQueries(fixture.Context);

            var result = await queries.QueryAsync<Widget>("SELECT Id, Name FROM Widgets ORDER BY Name");

            Assert.True(result.IsSuccess);
            Assert.Equal(["A", "B"], result.Value.Select(w => w.Name));
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_MatchingRow_ReturnsSome()
        {
            using var fixture = new SqliteFixture();
            fixture.Context.Widgets.Add(new Widget { Name = "A" });
            await fixture.Context.SaveChangesAsync();
            var queries = new DapperQueries(fixture.Context);

            var result = await queries.QueryFirstOrDefaultAsync<Widget>("SELECT Id, Name FROM Widgets WHERE Name = @Name", new { Name = "A" });

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.HasValue);
            Assert.Equal("A", result.Value.Value.Name);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_NoMatch_ReturnsNoneInsteadOfFailure()
        {
            using var fixture = new SqliteFixture();
            var queries = new DapperQueries(fixture.Context);

            var result = await queries.QueryFirstOrDefaultAsync<Widget>("SELECT Id, Name FROM Widgets WHERE Name = @Name", new { Name = "Missing" });

            Assert.True(result.IsSuccess);
            Assert.False(result.Value.HasValue);
        }

        [Fact]
        public async Task ExecuteAsync_UpdateStatement_ReturnsRowsAffected()
        {
            using var fixture = new SqliteFixture();
            fixture.Context.Widgets.Add(new Widget { Name = "A" });
            await fixture.Context.SaveChangesAsync();
            var queries = new DapperQueries(fixture.Context);

            var result = await queries.ExecuteAsync("UPDATE Widgets SET Name = @NewName WHERE Name = @OldName", new { NewName = "B", OldName = "A" });

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value);
        }

        [Fact]
        public async Task QueryAsync_InvalidSql_ReturnsFailureInsteadOfThrowing()
        {
            using var fixture = new SqliteFixture();
            var queries = new DapperQueries(fixture.Context);

            var result = await queries.QueryAsync<Widget>("SELECT * FROM NoSuchTable");

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task ExecuteAsync_InsideActiveEfCoreTransaction_ParticipatesInItAndRollsBackWithIt()
        {
            using var fixture = new SqliteFixture();
            var queries = new DapperQueries(fixture.Context);

            await using (var transaction = await fixture.Context.Database.BeginTransactionAsync())
            {
                await queries.ExecuteAsync("INSERT INTO Widgets (Name) VALUES (@Name)", new { Name = "RolledBack" });
                await transaction.RollbackAsync();
            }

            var afterRollback = await queries.QueryAsync<Widget>("SELECT Id, Name FROM Widgets");
            Assert.Empty(afterRollback.Value);
        }

        [Fact]
        public async Task ExecuteAsync_InsideCommittedEfCoreTransaction_Persists()
        {
            using var fixture = new SqliteFixture();
            var queries = new DapperQueries(fixture.Context);

            await using (var transaction = await fixture.Context.Database.BeginTransactionAsync())
            {
                await queries.ExecuteAsync("INSERT INTO Widgets (Name) VALUES (@Name)", new { Name = "Committed" });
                await transaction.CommitAsync();
            }

            var afterCommit = await queries.QueryAsync<Widget>("SELECT Id, Name FROM Widgets");
            Assert.Single(afterCommit.Value);
            Assert.Equal("Committed", afterCommit.Value[0].Name);
        }
    }
}
