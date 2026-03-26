namespace Pm.Api.Domain.Entities;

public sealed class BoardColumn
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Position { get; set; }

    public Board Board { get; set; } = null!;

    public List<CardItem> Cards { get; set; } = new();
}
