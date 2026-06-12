// ThreatCastPK.Web/Services/ThreatCastAuthStateProvider.cs
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ThreatCastPK.Web.Services;

public class ThreatCastAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;
    private bool _initialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public ThreatCastAuthStateProvider(AuthService authService)
    {
        _authService = authService;
        _authService.OnAuthStateChanged += NotifyAuthStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // If AuthService hasn't loaded from localStorage yet, do it now.
        // SemaphoreSlim prevents multiple simultaneous calls from initializing twice.
        if (!_initialized)
        {
            await _initLock.WaitAsync();
            try
            {
                if (!_initialized)
                {
                    await _authService.InitializeAsync();
                    _initialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        var user = _authService.GetCurrentUser();

        if (!user.IsLoggedIn)
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return new AuthenticationState(anonymous);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId ?? ""),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationState(principal);
    }

    // Also mark initialized when AuthService explicitly notifies —
    // covers the login/logout flow so we don't re-initialize after login
    private void NotifyAuthStateChanged()
    {
        _initialized = true;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}