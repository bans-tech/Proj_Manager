using Microsoft.EntityFrameworkCore;
using Pm.Api.Data;
using Pm.Api.Services;

namespace Pm.Api.Tests;

public sealed class BoardRepositoryTests
{
    [Fact]
    public async Task SeedMvpUserBoardAsync_CreatesUserBoardAndDefaultColumns()
    {
        await using var db = await CreateDbAsync();
        var repository = new BoardRepository(db);

        await repository.SeedMvpUserBoardAsync();
        var board = await repository.GetBoardForUserAsync("user");

        Assert.NotNull(board);
        Assert.Equal(5, board!.Columns.Count);
        Assert.Equal(new[] { "Backlog", "Ready", "In progress", "Review", "Done" }, board.Columns.Select(x => x.Name));
    }

    [Fact]
    public async Task RenameColumnAsync_UpdatesColumnNameForOwner()
    {
        await using var db = await CreateDbAsync();
        var repository = new BoardRepository(db);

        await repository.SeedMvpUserBoardAsync();
        var board = await repository.GetBoardForUserAsync("user");
        Assert.NotNull(board);

        var targetColumn = board!.Columns[0];
        var updated = await repository.RenameColumnAsync("user", targetColumn.Id, "Ideas");
        var after = await repository.GetBoardForUserAsync("user");

        Assert.True(updated);
        Assert.Equal("Ideas", after!.Columns.Single(x => x.Id == targetColumn.Id).Name);
    }

    [Fact]
    public async Task RenameColumnAsync_ReturnsFalseForDifferentUser()
    {
        await using var db = await CreateDbAsync();
        var repository = new BoardRepository(db);

        await repository.SeedMvpUserBoardAsync();
        var board = await repository.GetBoardForUserAsync("user");
        Assert.NotNull(board);

        var updated = await repository.RenameColumnAsync("other-user", board!.Columns[0].Id, "Ideas");

        Assert.False(updated);
    }

    [Fact]
    public async Task FileBackedSqlite_PersistsDataAcrossDbContextInstances()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pm-tests-persist-{Guid.NewGuid():N}.db");

        await using (var firstContext = await CreateDbAsync(dbPath))
        {
            var repository = new BoardRepository(firstContext);
            await repository.SeedMvpUserBoardAsync();
        }

        await using (var secondContext = await CreateDbAsync(dbPath))
        {
            var repository = new BoardRepository(secondContext);
            var board = await repository.GetBoardForUserAsync("user");

            Assert.NotNull(board);
            Assert.Equal(5, board!.Columns.Count);
        }
    }

    private static async Task<AppDbContext> CreateDbAsync(string? dbPath = null)
    {
        dbPath ??= Path.Combine(Path.GetTempPath(), $"pm-tests-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }
}
