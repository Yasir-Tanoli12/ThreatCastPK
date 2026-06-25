using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ThreatCastPK.API.Hubs;
using ThreatCastPK.API.Services;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.BackgroundServices;

public class CampaignDetectionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignDetectionBackgroundService> _logger;
    private readonly IHubContext<ThreatCastHub> _hubContext;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    public CampaignDetectionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CampaignDetectionBackgroundService> logger,
        IHubContext<ThreatCastHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[CampaignDetection] Service started.");

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunDetectionAsync(stoppingToken);
        }
    }

    private async Task RunDetectionAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ml = scope.ServiceProvider.GetRequiredService<MLService>();

            // Call the auto endpoint — ML service queries DB itself
            var result = await ml.DetectCampaignAutoAsync();

            if (result == null)
            {
                _logger.LogDebug("[CampaignDetection] ML service returned null.");
                return;
            }

            if (!result.IsCampaign)
            {
                _logger.LogDebug("[CampaignDetection] No campaign. Level: {Level}", result.AlertLevel);
                return;
            }

            _logger.LogWarning("[CampaignDetection] Campaign! Level: {Level}, Anomalies: {Count}/{Total}",
                result.AlertLevel, result.AnomalyCount, result.TotalEvents);

            // Broadcast to all connected clients
            await _hubContext.Clients.Group("all_viewers")
                .SendAsync("CampaignDetected", new
                {
                    alertLevel = result.AlertLevel,
                    anomalyCount = result.AnomalyCount,
                    totalEvents = result.TotalEvents,
                    affectedCities = result.AffectedCities,
                    affectedSectors = result.AffectedSectors,
                    message = result.Message,
                    detectedAt = DateTime.UtcNow
                }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CampaignDetection] Detection cycle failed.");
        }
    }
}