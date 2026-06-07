// ThreatCastPK.Web/Services/AuthService.cs
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace ThreatCastPK.Web.Services;

// Holds the data we get back from POST /api/auth/login
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

// Holds the current user's state — read by components and NavBar
public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsLoggedIn { get; set; } = false;
}

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    // Fires whenever login/logout happens — NavBar subscribes to this
    // to re-render without a full page refresh
    public event Action? OnAuthStateChanged;

    public void NotifyAuthStateChanged() => OnAuthStateChanged?.Invoke();

    // Cached in memory so we don't hit JS interop on every render
    private UserInfo _currentUser = new();

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    // Call this once when the app starts (from MainLayout.razor OnAfterRenderAsync)
    // Reads localStorage and restores the in-memory state
    public async Task InitializeAsync()
    {
        try
        {
            var info = await _js.InvokeAsync<JsonElement>("tcpkAuth.getUserInfo");
            var userId = info.GetProperty("userId").GetString();
            var username = info.GetProperty("username").GetString();
            var role = info.GetProperty("role").GetString();

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(username))
            {
                _currentUser = new UserInfo
                {
                    UserId = userId,
                    Username = username,
                    Role = role ?? "Public",
                    IsLoggedIn = true
                };
            }
        }
        catch
        {
            // JS interop can fail during prerender — safe to ignore,
            // InitializeAsync will be called again after render
            _currentUser = new UserInfo();
        }
    }

    // Returns the current user state (always safe to call — never throws)
    public UserInfo GetCurrentUser() => _currentUser;

    // Shorthand helpers used in pages and NavBar
    public bool IsLoggedIn => _currentUser.IsLoggedIn;
    public string Username => _currentUser.Username;
    public string Role => _currentUser.Role;
    public string UserId => _currentUser.UserId;

    public bool IsAdmin => _currentUser.Role == "Admin";
    public bool IsReporter => _currentUser.Role is "Reporter" or "Admin";

    // Returns the stored JWT token from localStorage
    // Used by ApiService to attach Authorization header
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("tcpkAuth.getToken");
        }
        catch
        {
            return null;
        }
    }

    // POST /api/auth/login
    // Returns null on success, error message string on failure
    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new
            {
                email,
                password
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result == null) return "Unexpected response from server.";

                // Persist to localStorage
                await _js.InvokeVoidAsync("tcpkAuth.setToken", result.Token);
                await _js.InvokeVoidAsync("tcpkAuth.setUserInfo",
                    result.UserId, result.Username, result.Role);

                // Update in-memory state
                _currentUser = new UserInfo
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    Role = result.Role,
                    IsLoggedIn = true
                };

                // Tell NavBar and other subscribers to re-render
                OnAuthStateChanged?.Invoke();
                return null; // null = success
            }

            if ((int)response.StatusCode == 403)
                return "Your account has been suspended.";

            return "Invalid email or password.";
        }
        catch
        {
            return "Unable to reach the server. Please try again.";
        }
    }

    // POST /api/auth/register
    // Returns null on success, error message string on failure
    public async Task<string?> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register", new
            {
                username,
                email,
                password
            });

            if (response.IsSuccessStatusCode)
                return null; // null = success

            // Try to read the error message from the response body
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (body.TryGetProperty("message", out var msg))
                return msg.GetString();

            return "Registration failed. Please try again.";
        }
        catch
        {
            return "Unable to reach the server. Please try again.";
        }
    }

    // Clears token and user info, fires state change
    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("tcpkAuth.clearAll");
        _currentUser = new UserInfo();
        OnAuthStateChanged?.Invoke();
    }
}