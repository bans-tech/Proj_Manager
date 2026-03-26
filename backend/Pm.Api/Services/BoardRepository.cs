using Microsoft.EntityFrameworkCore;
using Pm.Api.Data;
using Pm.Api.Domain.Entities;
using Pm.Api.Domain.Models;

namespace Pm.Api.Services;

public sealed class BoardRepository(AppDbContext db)
{
    public Task SeedMvpUserBoardAsync(CancellationToken cancellationToken = default)
        => SeedUserBoardAsync("user", cancellationToken);

    public async Task SeedUserBoardAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUsername))
            return;

        var user = await db.Users
            .Include(x => x.Board)
            .FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = normalizedUsername
            };
            db.Users.Add(user);
        }

        var board = user.Board;
        if (board is null)
        {
            board = new Board
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = "Main Board"
            };
            db.Boards.Add(board);
        }

        var existingColumns = await db.Columns
            .Where(x => x.BoardId == board.Id)
            .CountAsync(cancellationToken);

        if (existingColumns == 0)
        {
            var defaults = new[] { "Backlog", "Ready", "In progress", "Review", "Done" };
            for (var i = 0; i < defaults.Length; i++)
            {
                db.Columns.Add(new BoardColumn
                {
                    Id = Guid.NewGuid(),
                    BoardId = board.Id,
                    Name = defaults[i],
                    Position = i
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BoardSnapshot?> GetBoardForUserAsync(string username, CancellationToken cancellationToken = default)
    {
        var board = await db.Boards
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Columns)
            .Include(x => x.Cards)
            .FirstOrDefaultAsync(x => x.User.Username == username, cancellationToken);

        if (board is null)
            return null;

        var columns = board.Columns
            .OrderBy(x => x.Position)
            .Select(x => new BoardColumnSnapshot(x.Id, x.Name, x.Position))
            .ToList();

        var cards = board.Cards
            .OrderBy(x => x.Position)
            .Select(x => new CardSnapshot(x.Id, x.ColumnId, x.Title, x.Description, x.Position))
            .ToList();

        return new BoardSnapshot(board.Id, board.UserId, board.Name, columns, cards);
    }

    public async Task<bool> RenameColumnAsync(string username, Guid columnId, string newName, CancellationToken cancellationToken = default)
    {
        var normalized = (newName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var column = await db.Columns
            .Include(x => x.Board)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == columnId, cancellationToken);

        if (column is null || !string.Equals(column.Board.User.Username, username, StringComparison.Ordinal))
            return false;

        if (column.Name == normalized)
            return true;

        column.Name = normalized;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CardSnapshot?> AddCardAsync(string username, Guid columnId, string title, string description, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = (title ?? string.Empty).Trim();
        var normalizedDescription = (description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
            return null;

        var column = await db.Columns
            .Include(x => x.Board)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == columnId, cancellationToken);

        if (column is null || !string.Equals(column.Board.User.Username, username, StringComparison.Ordinal))
            return null;

        var position = await db.Cards
            .Where(x => x.ColumnId == columnId)
            .Select(x => (int?)x.Position)
            .MaxAsync(cancellationToken) ?? -1;

        var card = new CardItem
        {
            Id = Guid.NewGuid(),
            BoardId = column.BoardId,
            ColumnId = columnId,
            Title = normalizedTitle,
            Description = normalizedDescription,
            Position = position + 1
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync(cancellationToken);

        return new CardSnapshot(card.Id, card.ColumnId, card.Title, card.Description, card.Position);
    }

    public async Task<bool> UpdateCardAsync(string username, Guid cardId, string title, string description, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = (title ?? string.Empty).Trim();
        var normalizedDescription = (description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
            return false;

        var card = await db.Cards
            .Include(x => x.Board)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

        if (card is null || !string.Equals(card.Board.User.Username, username, StringComparison.Ordinal))
            return false;

        card.Title = normalizedTitle;
        card.Description = normalizedDescription;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCardAsync(string username, Guid cardId, CancellationToken cancellationToken = default)
    {
        var card = await db.Cards
            .Include(x => x.Board)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

        if (card is null || !string.Equals(card.Board.User.Username, username, StringComparison.Ordinal))
            return false;

        var sourceColumnId = card.ColumnId;
        db.Cards.Remove(card);

        var sourceCards = await db.Cards
            .Where(x => x.ColumnId == sourceColumnId)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < sourceCards.Count; i++)
            sourceCards[i].Position = i;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MoveCardAsync(string username, Guid cardId, Guid targetColumnId, int targetPosition, CancellationToken cancellationToken = default)
    {
        var card = await db.Cards
            .Include(x => x.Board)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

        if (card is null || !string.Equals(card.Board.User.Username, username, StringComparison.Ordinal))
            return false;

        var targetColumn = await db.Columns
            .Include(x => x.Board)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == targetColumnId, cancellationToken);

        if (targetColumn is null || !string.Equals(targetColumn.Board.User.Username, username, StringComparison.Ordinal))
            return false;

        if (targetColumn.BoardId != card.BoardId)
            return false;

        var sourceColumnId = card.ColumnId;

        if (sourceColumnId == targetColumnId)
        {
            var cardsInColumn = await db.Cards
                .Where(x => x.ColumnId == sourceColumnId)
                .OrderBy(x => x.Position)
                .ToListAsync(cancellationToken);

            cardsInColumn.RemoveAll(x => x.Id == card.Id);
            targetPosition = Math.Clamp(targetPosition, 0, cardsInColumn.Count);
            cardsInColumn.Insert(targetPosition, card);

            for (var i = 0; i < cardsInColumn.Count; i++)
                cardsInColumn[i].Position = i;

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var sourceCards = await db.Cards
            .Where(x => x.ColumnId == sourceColumnId && x.Id != card.Id)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < sourceCards.Count; i++)
            sourceCards[i].Position = i;

        var targetCards = await db.Cards
            .Where(x => x.ColumnId == targetColumnId && x.Id != card.Id)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);

        targetPosition = Math.Clamp(targetPosition, 0, targetCards.Count);
        card.ColumnId = targetColumnId;
        targetCards.Insert(targetPosition, card);

        for (var i = 0; i < targetCards.Count; i++)
            targetCards[i].Position = i;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
