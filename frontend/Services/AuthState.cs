using System.Net;
using System.Net.Http.Json;

namespace frontend.Services;

public sealed class AuthState(HttpClient http)
{
    public event Action? Changed;

    public bool IsChecking { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string ErrorMessage { get; private set; } = string.Empty;

    public async Task RefreshAsync()
    {
        IsChecking = true;
        ErrorMessage = string.Empty;
        Changed?.Invoke();

        try
        {
            var response = await http.GetAsync("/api/auth/me");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                IsAuthenticated = false;
                Username = string.Empty;
                return;
            }

            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<AuthMeResponse>();
            IsAuthenticated = payload?.IsAuthenticated == true;
            Username = payload?.Username ?? string.Empty;
        }
        catch
        {
            IsAuthenticated = false;
            Username = string.Empty;
            ErrorMessage = "Unable to check auth status.";
        }
        finally
        {
            IsChecking = false;
            Changed?.Invoke();
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        ErrorMessage = string.Empty;
        Changed?.Invoke();

        try
        {
            var response = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ErrorMessage = "Invalid username or password.";
                Changed?.Invoke();
                return false;
            }

            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<AuthMeResponse>();
            IsAuthenticated = payload?.IsAuthenticated == true;
            Username = payload?.Username ?? username;
            Changed?.Invoke();
            return IsAuthenticated;
        }
        catch
        {
            ErrorMessage = "Sign-in failed. Try again.";
            Changed?.Invoke();
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await http.PostAsync("/api/auth/logout", content: null);
        }
        finally
        {
            IsAuthenticated = false;
            Username = string.Empty;
            ErrorMessage = string.Empty;
            Changed?.Invoke();
        }
    }

    private sealed record LoginRequest(string Username, string Password);

    private sealed record AuthMeResponse(bool IsAuthenticated, string Username);
}
