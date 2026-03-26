using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pm.Api.Tests;

public class HealthEndpointTests(WebApplicationFactory<Program> factory)
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
}
