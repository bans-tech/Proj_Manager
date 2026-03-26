using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pm.Api.Data;
using Pm.Api.Services;

namespace Pm.Api.Tests;

public class ApiEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetHealth_ReturnsOkStatusPayload()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);

        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("pm-api", doc.RootElement.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Root_ReturnsBlazorAppShell()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<div id=\"app\">", html);
        Assert.Contains("blazor.webassembly.js", html);
    }

    [Fact]
    public async Task StaticAsset_IsServedFromFrontendWwwroot()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/css/app.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AuthMe_WithoutCookie_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_AllowsMeEndpoint()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", "password"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var payload = await me.Content.ReadFromJsonAsync<AuthMeResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.IsAuthenticated);
        Assert.Equal("user", payload.Username);
    }

    [Fact]
    public async Task Logout_AfterLogin_RevokesSession()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", "password"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var meAfterLogout = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);
    }

    [Fact]
    public async Task BoardEndpoint_RequiresAuthentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/board");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BoardEndpoint_AfterLogin_ReturnsBoardSnapshot()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/board");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var board = await response.Content.ReadFromJsonAsync<BoardResponse>();
        Assert.NotNull(board);
        Assert.Equal(5, board!.Columns.Count);
    }

    [Fact]
    public async Task RenameColumn_UpdatesBoardState()
    {
        var client = await CreateAuthenticatedClientAsync();
        var board = await GetBoardAsync(client);
        var column = board.Columns[0];

        var rename = await client.PatchAsJsonAsync($"/api/columns/{column.Id}", new RenameColumnRequest("Ideas"));

        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);

        var after = await GetBoardAsync(client);
        Assert.Equal("Ideas", after.Columns.Single(x => x.Id == column.Id).Name);
    }

    [Fact]
    public async Task CardCrudAndMove_Works()
    {
        var client = await CreateAuthenticatedClientAsync();
        var board = await GetBoardAsync(client);
        var sourceColumn = board.Columns[0];
        var targetColumn = board.Columns[1];

        var create = await client.PostAsJsonAsync("/api/cards", new CreateCardRequest(sourceColumn.Id, "API card", "details"));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        var createdCard = await create.Content.ReadFromJsonAsync<CardResponse>();
        Assert.NotNull(createdCard);

        var move = await client.PostAsJsonAsync($"/api/cards/{createdCard!.Id}/move", new MoveCardRequest(targetColumn.Id, 0));
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);

        var update = await client.PutAsJsonAsync($"/api/cards/{createdCard.Id}", new UpdateCardRequest("API card updated", "details updated"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var boardAfterUpdate = await GetBoardAsync(client);
        var updatedCard = boardAfterUpdate.Cards.Single(x => x.Id == createdCard.Id);
        Assert.Equal(targetColumn.Id, updatedCard.ColumnId);
        Assert.Equal("API card updated", updatedCard.Title);

        var delete = await client.DeleteAsync($"/api/cards/{createdCard.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var boardAfterDelete = await GetBoardAsync(client);
        Assert.DoesNotContain(boardAfterDelete.Cards, x => x.Id == createdCard.Id);
    }

    [Fact]
    public async Task RenameColumn_ForDifferentUserResource_ReturnsNotFound()
    {
        var foreignColumnId = await CreateOtherUserColumnAsync();
        var client = await CreateAuthenticatedClientAsync();

        var rename = await client.PatchAsJsonAsync($"/api/columns/{foreignColumnId}", new RenameColumnRequest("Should not work"));

        Assert.Equal(HttpStatusCode.NotFound, rename.StatusCode);
    }

    private async Task<Guid> CreateOtherUserColumnAsync()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<BoardRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await repository.SeedUserBoardAsync("other-user");
        var board = await repository.GetBoardForUserAsync("other-user");

        return board!.Columns[0].Id;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", "password"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private static async Task<BoardResponse> GetBoardAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/board");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var board = await response.Content.ReadFromJsonAsync<BoardResponse>();
        Assert.NotNull(board);
        return board!;
    }

    private sealed record LoginRequest(string Username, string Password);

    private sealed record AuthMeResponse(bool IsAuthenticated, string Username);

    private sealed record RenameColumnRequest(string Name);

    private sealed record CreateCardRequest(Guid ColumnId, string Title, string Description);

    private sealed record UpdateCardRequest(string Title, string Description);

    private sealed record MoveCardRequest(Guid ColumnId, int Position);

    private sealed record ColumnResponse(Guid Id, string Name, int Position);

    private sealed record CardResponse(Guid Id, Guid ColumnId, string Title, string Description, int Position);

    private sealed record BoardResponse(Guid Id, Guid UserId, string BoardName, List<ColumnResponse> Columns, List<CardResponse> Cards);
}
