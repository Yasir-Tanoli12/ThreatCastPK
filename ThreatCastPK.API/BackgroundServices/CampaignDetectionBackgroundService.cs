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
            var db = scope.ServiceProvider.GetRequiredService<ThreatCastDbContext>();
            var ml = scope.ServiceProvider.GetRequiredService<MLService>();

            // Get events from last 6 hours
            var cutoff = DateTime.UtcNow.AddHours(-6);
            var recentEvents = await db.AttackEvents
                .Include(e => e.Location)
                .Where(e => e.OccurredAt >= cutoff)
                .ToListAsync(ct);

            if (recentEvents.Count < 5)
            {
                _logger.LogDebug("[CampaignDetection] Not enough events ({Count}) for detection.", recentEvents.Count);
                return;
            }

            // Build ML input from real events
            var mlInputs = recentEvents.Select(e => new AttackEventInput
            {
                AttackType = e.AttackType.ToString(),
                AnomalyScore = e.Severity / 5.0,      // normalize severity to 0-1
                PacketLength = 500,                    // default — we don't store this
                Protocol = "TCP",                  // default
                GeoLocation = e.Location?.CityName ?? "Unknown",
                NetworkSegment = e.TargetSector.ToString()
            }).ToList();

            var result = await ml.DetectCampaignAsync(mlInputs);

            if (result == null || !result.IsCampaign)
            {
                _logger.LogDebug("[CampaignDetection] No campaign detected.");
                return;
            }

            _logger.LogWarning("[CampaignDetection] Campaign detected! Level: {Level}, Anomalies: {Count}/{Total}",
                result.AlertLevel, result.AnomalyCount, result.TotalEvents);

            // Get anomalous events
            var anomalousEvents = recentEvents
                .Where((e, i) => i < result.AnomalyFlags.Count && result.AnomalyFlags[i])
                .ToList();

            var affectedCities = string.Join(",",
                anomalousEvents.Select(e => e.Location?.CityName ?? "")
                               .Where(c => !string.IsNullOrEmpty(c))
                               .Distinct());

            var affectedSectors = string.Join(",",
                anomalousEvents.Select(e => e.TargetSector.ToString()).Distinct());

            // Parse alert level
            var alertLevel = result.AlertLevel switch
            {
                "CRITICAL" => AlertLevel.Critical,
                "HIGH" => AlertLevel.High,
                _ => AlertLevel.Medium
            };

            // Check if we already recorded a campaign in the last hour
            // to avoid spamming duplicate records
            var recentCampaign = await db.ThreatCampaigns
                .AnyAsync(c => c.DetectedAt >= DateTime.UtcNow.AddHours(-1), ct);

            if (!recentCampaign)
            {
                var campaign = new ThreatCampaign
                {
                    Id = Guid.NewGuid(),
                    IpRange = "Multiple",
                    DetectedAt = DateTime.UtcNow,
                    AffectedCities = affectedCities,
                    AffectedSectors = affectedSectors,
                    ReportCount = result.AnomalyCount,
                    AlertLevel = alertLevel
                };

                db.ThreatCampaigns.Add(campaign);
                await db.SaveChangesAsync(ct);
            }

            // Broadcast campaign banner to ALL connected clients via SignalR
            await _hubContext.Clients.Group("all_viewers")
                .SendAsync("CampaignDetected", new
                {
                    alertLevel = result.AlertLevel,
                    anomalyCount = result.AnomalyCount,
                    totalEvents = result.TotalEvents,
                    affectedCities = affectedCities,
                    affectedSectors = affectedSectors,
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