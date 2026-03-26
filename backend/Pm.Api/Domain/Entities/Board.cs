namespace Pm.Api.Domain.Entities;

public sealed class Board
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public List<BoardColumn> Columns { get; set; } = new();

    public List<CardItem> Cards { get; set; } = new();
}
