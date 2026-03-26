using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Pm.Api.Data;
using Pm.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=pm.db";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<BoardRepository>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "pm_auth";
        options.LoginPath = "/";
        options.AccessDeniedPath = "/";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")))
    .SetApplicationName("pm");

var app = builder.Build();

if (ShouldAutoMigrate(app.Environment))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var boardRepository = scope.ServiceProvider.GetRequiredService<BoardRepository>();
    await boardRepository.SeedMvpUserBoardAsync();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Json(new
{
    status = "ok",
    service = "pm-api",
    utcTime = DateTime.UtcNow
}, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

app.MapPost("/api/auth/login", async (LoginRequest request, HttpContext httpContext) =>
{
    if (request.Username != "user" || request.Password != "password")
        return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, "mvp-user"),
        new(ClaimTypes.Name, request.Username)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        });

    return Results.Ok(new { isAuthenticated = true, username = request.Username });
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { isAuthenticated = false });
});

app.MapGet("/api/auth/me", [Authorize] (HttpContext httpContext) =>
{
    var username = httpContext.User.Identity?.Name ?? "user";
    return Results.Ok(new { isAuthenticated = true, username });
});

app.MapGet("/api/board", [Authorize] async (HttpContext httpContext, BoardRepository boards) =>
{
    var username = GetUsername(httpContext);
    if (username is null)
        return Results.Unauthorized();

    var board = await boards.GetBoardForUserAsync(username);
    return board is null ? Results.NotFound() : Results.Ok(board);
});

app.MapPatch("/api/columns/{columnId:guid}", [Authorize] async (Guid columnId, RenameColumnRequest request, HttpContext httpContext, BoardRepository boards) =>
{
    var username = GetUsername(httpContext);
    if (username is null)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Column name is required." });

    var updated = await boards.RenameColumnAsync(username, columnId, request.Name);
    return updated ? Results.Ok() : Results.NotFound();
});

app.MapPost("/api/cards", [Authorize] async (CreateCardRequest request, HttpContext httpContext, BoardRepository boards) =>
{
    var username = GetUsername(httpContext);
    if (username is null)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Card title is required." });

    var card = await boards.AddCardAsync(username, request.ColumnId, request.Title, request.Description ?? string.Empty);
    return card is null ? Results.NotFound() : Results.Ok(card);
});

app.MapPut("/api/cards/{cardId:guid}", [Authorize] async (Guid cardId, UpdateCardRequest request, HttpContext httpContext, BoardRepository boards) =>
{
    var username = GetUsername(httpContext);
    if (username is null)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Card title is required." });

    var updated = await boards.UpdateCardAsync(username, cardId, request.Title, request.Description ?? string.Empty);
    return updated ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/cards/{cardId:guid}", [Authorize] async (Guid cardId, HttpContext httpContext, BoardRepository boards) =>
{
    var username = GetUsername(httpContext);
    if (username is null)
        return Results.Unauthorized();

    var deleted = await boards.DeleteCardAsync(username, cardId);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/api/cards/{cardId:guid}/move", [Authorize] async (Guid cardId, MoveCardRequest request, HttpContext httpContext, BoardRepository boards) =>
{
    var username = GetUsername(httpContext);
    if (username is null)
        return Results.Unauthorized();

    var moved = await boards.MoveCardAsync(username, cardId, request.ColumnId, request.Position);
    return moved ? Results.Ok() : Results.NotFound();
});

app.MapFallbackToFile("index.html");

app.Run();

static bool ShouldAutoMigrate(IHostEnvironment environment)
    => environment.IsDevelopment() || string.Equals(environment.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase);

static string? GetUsername(HttpContext httpContext)
    => httpContext.User.Identity?.Name;

public sealed record LoginRequest(string Username, string Password);

public sealed record RenameColumnRequest(string Name);

public sealed record CreateCardRequest(Guid ColumnId, string Title, string? Description);

public sealed record UpdateCardRequest(string Title, string? Description);

public sealed record MoveCardRequest(Guid ColumnId, int Position);

public partial class Program;
