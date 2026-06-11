// ThreatCastPK.Web/Services/ThreatCastAuthStateProvider.cs
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ThreatCastPK.Web.Services;

public class ThreatCastAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;

    public ThreatCastAuthStateProvider(AuthService authService)
    {
        _authService = authService;

        // When AuthService says login/logout happened,
        // tell Blazor to re-evaluate auth state everywhere
        _authService.OnAuthStateChanged += NotifyAuthStateChanged;
    }

    // Blazor calls this whenever it needs to know who is logged in.
    // We build a ClaimsPrincipal from our AuthService's in-memory state.
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _authService.GetCurrentUser();

        if (!user.IsLoggedIn)
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.UserId ?? ""),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(principal));
    }

    // Called by AuthService.OnAuthStateChanged
    // Triggers Blazor to re-check auth on NavBar, pages with [Authorize], etc.
    private void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}