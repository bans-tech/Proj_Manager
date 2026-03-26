namespace frontend.Models;

public sealed record CardModel(
    Guid Id,
    Guid ColumnId,
    string Title,
    string Details,
    int Order
);

