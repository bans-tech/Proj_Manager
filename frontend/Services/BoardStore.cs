using frontend.Models;

namespace frontend.Services;

public sealed class BoardStore
{
    private readonly List<ColumnModel> _columns;
    private readonly List<CardModel> _cards;

    public BoardStore()
    {
        (_columns, _cards) = Seed();
    }

    public event Action? Changed;

    public BoardSnapshot GetSnapshot()
        => new(_columns.ToList(), _cards.ToList());

    public void RenameColumn(Guid columnId, string newName)
    {
        newName = (newName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(newName))
            return;

        var index = _columns.FindIndex(c => c.Id == columnId);
        if (index < 0)
            return;

        if (_columns[index].Name == newName)
            return;

        _columns[index] = _columns[index] with { Name = newName };
        Changed?.Invoke();
    }

    public Guid AddCard(Guid columnId, string title, string details)
    {
        title = (title ?? string.Empty).Trim();
        details = (details ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(title))
            return Guid.Empty;

        if (!_columns.Any(c => c.Id == columnId))
            return Guid.Empty;

        var nextOrder = _cards
            .Where(c => c.ColumnId == columnId)
            .Select(c => c.Order)
            .DefaultIfEmpty(-1)
            .Max() + 1;

        var id = Guid.NewGuid();
        _cards.Add(new CardModel(id, columnId, title, details, nextOrder));
        Changed?.Invoke();
        return id;
    }

    public void DeleteCard(Guid cardId)
    {
        var removed = _cards.RemoveAll(c => c.Id == cardId);
        if (removed > 0)
            Changed?.Invoke();
    }

    public void MoveCard(Guid cardId, Guid targetColumnId, int targetIndex)
    {
        if (!_columns.Any(c => c.Id == targetColumnId))
            return;

        var cardIndex = _cards.FindIndex(c => c.Id == cardId);
        if (cardIndex < 0)
            return;

        var card = _cards[cardIndex];

        var targetCards = _cards
            .Where(c => c.ColumnId == targetColumnId && c.Id != cardId)
            .OrderBy(c => c.Order)
            .ToList();

        targetIndex = Math.Clamp(targetIndex, 0, targetCards.Count);
        targetCards.Insert(targetIndex, card with { ColumnId = targetColumnId });

        NormalizeOrder(targetColumnId, targetCards);

        if (card.ColumnId != targetColumnId)
        {
            var sourceCards = _cards
                .Where(c => c.ColumnId == card.ColumnId && c.Id != cardId)
                .OrderBy(c => c.Order)
                .ToList();

            NormalizeOrder(card.ColumnId, sourceCards);
        }

        Changed?.Invoke();
    }

    private void NormalizeOrder(Guid columnId, List<CardModel> ordered)
    {
        var next = new List<CardModel>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
            next.Add(ordered[i] with { ColumnId = columnId, Order = i });

        _cards.RemoveAll(c => c.ColumnId == columnId);
        _cards.AddRange(next);
    }

    private static (List<ColumnModel> columns, List<CardModel> cards) Seed()
    {
        var columns = new List<ColumnModel>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Backlog"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Ready"),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "In progress"),
            new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Review"),
            new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Done"),
        };

        var cards = new List<CardModel>();

        void Add(string columnName, string title, string details)
        {
            var columnId = columns.Single(c => c.Name == columnName).Id;
            var order = cards.Count(c => c.ColumnId == columnId);
            cards.Add(new CardModel(Guid.NewGuid(), columnId, title, details, order));
        }

        Add("Backlog", "Landing polish", "Tighten spacing, typography, and column empty states.");
        Add("Ready", "DnD feel", "Make drag/drop smooth with clear drop indicators.");
        Add("In progress", "Kanban MVP", "Single board, five columns, no persistence.");
        Add("Review", "Test coverage", "Unit tests for store and E2E for key flows.");
        Add("Done", "Design tokens", "Colors aligned to the provided palette.");

        return (columns, cards);
    }
}

