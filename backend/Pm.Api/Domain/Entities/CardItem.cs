namespace Pm.Api.Domain.Entities;

public sealed class CardItem
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Guid ColumnId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Position { get; set; }

    public Board Board { get; set; } = null!;

    public BoardColumn Column { get; set; } = null!;
}
