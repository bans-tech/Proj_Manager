using System.Net;
using System.Net.Http.Json;
using frontend.Models;

namespace frontend.Services;

public sealed class BoardStore(HttpClient http)
{
    private readonly List<ColumnModel> _columns = new();
    private readonly List<CardModel> _cards = new();

    public event Action? Changed;

    public bool IsBusy { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    public BoardSnapshot GetSnapshot()
        => new(_columns.ToList(), _cards.OrderBy(x => x.Order).ToList());

    public void Clear()
    {
        _columns.Clear();
        _cards.Clear();
        ErrorMessage = string.Empty;
        Changed?.Invoke();
    }

    public Task<bool> LoadAsync()
        => ExecuteAsync(async () =>
        {
            var response = await http.GetAsync("/api/board");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _columns.Clear();
                _cards.Clear();
                ErrorMessage = string.Empty;
                return true;
            }

            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<BoardResponse>();
            if (payload is null)
            {
                ErrorMessage = "Failed to load board.";
                return false;
            }

            _columns.Clear();
            _columns.AddRange(payload.Columns.OrderBy(x => x.Position).Select(x => new ColumnModel(x.Id, x.Name)));

            _cards.Clear();
            _cards.AddRange(payload.Cards
                .OrderBy(x => x.Position)
                .Select(x => new CardModel(x.Id, x.ColumnId, x.Title, x.Description, x.Position)));

            return true;
        });

    public Task<bool> RenameColumnAsync(Guid columnId, string newName)
        => ExecuteMutationAsync(async () =>
        {
            var response = await http.PatchAsJsonAsync($"/api/columns/{columnId}", new RenameColumnRequest(newName));
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = response.StatusCode == HttpStatusCode.BadRequest
                    ? "Invalid column name."
                    : "Unable to rename column.";
                return false;
            }

            return true;
        });

    public async Task<Guid> AddCardAsync(Guid columnId, string title, string details)
    {
        var id = Guid.Empty;

        var success = await ExecuteMutationAsync(async () =>
        {
            var response = await http.PostAsJsonAsync("/api/cards", new CreateCardRequest(columnId, title, details));
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = response.StatusCode == HttpStatusCode.BadRequest
                    ? "Card title is required."
                    : "Unable to add card.";
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<CardResponse>();
            id = payload?.Id ?? Guid.Empty;
            return id != Guid.Empty;
        });

        return success ? id : Guid.Empty;
    }

    public Task<bool> UpdateCardAsync(Guid cardId, string title, string details)
        => ExecuteMutationAsync(async () =>
        {
            var response = await http.PutAsJsonAsync($"/api/cards/{cardId}", new UpdateCardRequest(title, details));
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = response.StatusCode == HttpStatusCode.BadRequest
                    ? "Card title is required."
                    : "Unable to update card.";
                return false;
            }

            return true;
        });

    public Task<bool> DeleteCardAsync(Guid cardId)
        => ExecuteMutationAsync(async () =>
        {
            var response = await http.DeleteAsync($"/api/cards/{cardId}");
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                ErrorMessage = "Unable to delete card.";
                return false;
            }

            return true;
        });

    public Task<bool> MoveCardAsync(Guid cardId, Guid targetColumnId, int targetIndex)
        => ExecuteMutationAsync(async () =>
        {
            var response = await http.PostAsJsonAsync($"/api/cards/{cardId}/move", new MoveCardRequest(targetColumnId, targetIndex));
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Unable to move card.";
                return false;
            }

            return true;
        });

    private async Task<bool> ExecuteMutationAsync(Func<Task<bool>> operation)
    {
        var ok = await ExecuteAsync(operation);
        if (!ok)
            return false;

        return await LoadAsync();
    }

    private async Task<bool> ExecuteAsync(Func<Task<bool>> operation)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        Changed?.Invoke();

        try
        {
            return await operation();
        }
        catch
        {
            ErrorMessage = "A network error occurred.";
            return false;
        }
        finally
        {
            IsBusy = false;
            Changed?.Invoke();
        }
    }

    private sealed record RenameColumnRequest(string Name);

    private sealed record CreateCardRequest(Guid ColumnId, string Title, string Description);

    private sealed record UpdateCardRequest(string Title, string Description);

    private sealed record MoveCardRequest(Guid ColumnId, int Position);

    private sealed record ColumnResponse(Guid Id, string Name, int Position);

    private sealed record CardResponse(Guid Id, Guid ColumnId, string Title, string Description, int Position);

    private sealed record BoardResponse(Guid Id, Guid UserId, string BoardName, List<ColumnResponse> Columns, List<CardResponse> Cards);
}
