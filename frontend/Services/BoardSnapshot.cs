using frontend.Models;

namespace frontend.Services;

public sealed record BoardSnapshot(IReadOnlyList<ColumnModel> Columns, IReadOnlyList<CardModel> Cards);

