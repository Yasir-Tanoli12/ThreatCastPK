// ThreatCastPK.Web/Services/AuthService.cs
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace ThreatCastPK.Web.Services;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

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

    public event Action? OnAuthStateChanged;
    public void NotifyAuthStateChanged() => OnAuthStateChanged?.Invoke();

    private UserInfo _currentUser = new();
    private bool _initialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    // Call this once from MainLayout or ThreatCastAuthStateProvider
    public async Task InitializeAsync()
    {
        // Guard: only initialize once per circuit lifetime
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            try
            {
                var info = await _js.InvokeAsync<JsonElement>("tcpkAuth.getUserInfo");
                var userId = info.GetProperty("userId").GetString();
                var username = info.GetProperty("username").GetString();
                var role = info.GetProperty("role").GetString();

                if (!string.IsNullOrEmpty(username))
                {
                    _currentUser = new UserInfo
                    {
                        UserId = userId ?? string.Empty,
                        Username = username,
                        Role = role ?? "Public",
                        IsLoggedIn = true
                    };
                }
            }
            catch
            {
                // JS interop not available during prerender — safe to ignore
                _currentUser = new UserInfo();
                return;
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // Used by NavBar and pages — ensures auth is loaded before returning state
    // This is the key fix: if somehow _currentUser is blank and we haven't
    // initialized, try again rather than silently returning empty state
    public async Task<UserInfo> GetCurrentUserAsync()
    {
        if (!_initialized)
            await InitializeAsync();
        return _currentUser;
    }

    // Synchronous version — only use where async isn't possible
    // Will return blank if InitializeAsync hasn't been called yet
    public UserInfo GetCurrentUser() => _currentUser;

    public bool IsLoggedIn => _currentUser.IsLoggedIn;
    public string Username => _currentUser.Username;
    public string Role => _currentUser.Role;
    public string UserId => _currentUser.UserId;

    public bool IsAdmin => _currentUser.Role == "Admin";
    public bool IsReporter => _currentUser.Role is "Reporter" or "Admin";

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("tcpkAuth.getToken");
        }
        catch { return null; }
    }

    public async Task<(string? error, string? errorCode)> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result == null) return ("Unexpected response from server.", null);

                await _js.InvokeVoidAsync("tcpkAuth.clearAll");
                await _js.InvokeVoidAsync("tcpkAuth.setToken", result.Token);
                await _js.InvokeVoidAsync("tcpkAuth.setUserInfo",
                    result.UserId, result.Username, result.Role);

                _currentUser = new UserInfo
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    Role = result.Role,
                    IsLoggedIn = true
                };

                _initialized = true;
                OnAuthStateChanged?.Invoke();
                return (null, null);  // success
            }

            // Read the error body to check for error code
            try
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                var message = body.TryGetProperty("message", out var msg)
                    ? msg.GetString()
                    : "Invalid email or password.";
                var code = body.TryGetProperty("code", out var c)
                    ? c.GetString()
                    : null;
                return (message, code);
            }
            catch
            {
                return ("Invalid email or password.", null);
            }
        }
        catch
        {
            return ("Unable to reach the server. Please try again.", null);
        }
    }

    public async Task<bool> ResendVerificationAsync(string email)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/resend-verification", new { email });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register",
                new { username, email, password });

            if (response.IsSuccessStatusCode) return null;

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

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("tcpkAuth.clearAll");
        _currentUser = new UserInfo();
        _initialized = true; // Still mark initialized — we know the state (logged out)
        OnAuthStateChanged?.Invoke();
    }
}