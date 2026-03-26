namespace Pm.Api.Domain.Models;

public sealed record BoardColumnSnapshot(Guid Id, string Name, int Position);

public sealed record CardSnapshot(Guid Id, Guid ColumnId, string Title, string Description, int Position);

public sealed record BoardSnapshot(Guid Id, Guid UserId, string BoardName, IReadOnlyList<BoardColumnSnapshot> Columns, IReadOnlyList<CardSnapshot> Cards);
