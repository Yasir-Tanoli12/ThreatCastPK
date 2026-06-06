// ThreatCastPK.Web/Services/SignalRService.cs
// Manages a single SignalR connection shared across the whole app.
// Pages subscribe to events through this service instead of
// creating their own HubConnection (which would waste connections).

using Microsoft.AspNetCore.SignalR.Client;

namespace ThreatCastPK.Web.Services;

public class AttackEventPayload
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public int Severity { get; set; }
    public DateTime OccurredAt { get; set; }
    public string ConfidenceTier { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    // Coordinates for heatmap — populated from seeded Locations table
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly IConfiguration _config;
    private readonly AuthService _auth;

    // Pages subscribe to this to receive new attack events
    public event Func<AttackEventPayload, Task>? OnNewAttackEvent;

    // Pages subscribe to this to receive raw notification strings
    public event Func<string, Task>? OnNewNotification;

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public SignalRService(IConfiguration config, AuthService auth)
    {
        _config = config;
        _auth = auth;
    }

    // Called from MainLayout.razor OnAfterRenderAsync after auth is initialized
    public async Task StartAsync()
    {
        if (_connection != null) return; // already started

        var hubUrl = _config["SignalR:HubUrl"]
                     ?? "http://localhost:5262/hubs/threatcast";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Attach JWT so the hub can identify the user
                options.AccessTokenProvider = async () =>
                    await _auth.GetTokenAsync();
            })
            .WithAutomaticReconnect() // retries: 0s, 2s, 10s, 30s
            .Build();

        // Wire up incoming server events
        _connection.On<AttackEventPayload>("NewAttackEvent", async payload =>
        {
            if (OnNewAttackEvent != null)
                await OnNewAttackEvent.Invoke(payload);
        });

        _connection.On<string>("NewNotification", async message =>
        {
            if (OnNewNotification != null)
                await OnNewNotification.Invoke(message);
        });

        // Log reconnection state changes (useful during dev)
        _connection.Reconnecting += error =>
        {
            Console.WriteLine($"[SignalR] Reconnecting: {error?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Console.WriteLine($"[SignalR] Reconnected: {connectionId}");
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            Console.WriteLine("[SignalR] Connected.");

            // If user is logged in, join their personal notification group
            if (_auth.IsLoggedIn)
                await JoinUserGroupAsync(_auth.UserId);
        }
        catch (Exception ex)
        {
            // Connection failure is non-fatal — map still works,
            // just won't get real-time updates
            Console.WriteLine($"[SignalR] Failed to connect: {ex.Message}");
        }
    }

    // Join a city-specific group to receive filtered events
    public async Task JoinCityGroupAsync(string city)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync("JoinCityGroup", city);
    }

    public async Task LeaveCityGroupAsync(string city)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync("LeaveCityGroup", city);
    }

    // Join a sector-specific group
    public async Task JoinSectorGroupAsync(string sector)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync("JoinSectorGroup", sector);
    }

    public async Task LeaveSectorGroupAsync(string sector)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync("LeaveSectorGroup", sector);
    }

    // Join the user's personal group for notifications
    public async Task JoinUserGroupAsync(string userId)
    {
        if (!IsConnected || string.IsNullOrEmpty(userId)) return;
        try
        {
            await _connection!.InvokeAsync("JoinUserGroup", userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] JoinUserGroup failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}