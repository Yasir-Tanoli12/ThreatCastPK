// File Path: ThreatCastPK.Web/Services/SignalRService.cs
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
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ThreatCampaignPayload
{
    // Fields from SignalR broadcast (CampaignDetected)
    public string AlertLevel { get; set; } = string.Empty;
    public int AnomalyCount { get; set; }
    public int TotalEvents { get; set; }
    public string AffectedCities { get; set; } = string.Empty;
    public string AffectedSectors { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public Guid Id { get; set; }
    public string IpRange { get; set; } = string.Empty;
    public int ReportCount { get; set; }
}

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly IConfiguration _config;
    private readonly AuthService _auth;

    public event Func<AttackEventPayload, Task>? OnNewAttackEvent;
    public event Func<NotificationPayload, Task>? OnNewNotification;
    public event Func<ThreatCampaignPayload, Task>? OnNewThreatCampaign; // Event handler

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public SignalRService(IConfiguration config, AuthService auth)
    {
        _config = config;
        _auth = auth;
    }

    public async Task StartAsync()
    {
        if (_connection != null) return;

        var hubUrl = _config["SignalR:HubUrl"]
                     ?? "http://localhost:5262/hubs/threatcast";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await _auth.GetTokenAsync();
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                        clientHandler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    return handler;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<AttackEventPayload>("NewAttackEvent", async payload =>
        {
            if (OnNewAttackEvent != null)
                await OnNewAttackEvent.Invoke(payload);
        });

        _connection.On<NotificationPayload>("NewNotification", async payload =>
        {
            if (OnNewNotification != null)
                await OnNewNotification.Invoke(payload);
        });

        // Register campaign listener
        _connection.On<ThreatCampaignPayload>("CampaignDetected", async payload =>
        {
            if (OnNewThreatCampaign != null)
                await OnNewThreatCampaign.Invoke(payload);
        });

        try
        {
            await _connection.StartAsync();
            if (_auth.IsLoggedIn)
            {
                await JoinUserGroupAsync(_auth.UserId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Failed to connect: {ex.Message}");
        }
    }

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