using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Json(new
{
    status = "ok",
    service = "pm-api",
    utcTime = DateTime.UtcNow
}, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
