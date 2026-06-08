// File Path: ThreatCastPK.API/BackgroundServices/CampaignDetectionBackgroundService.cs
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
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public CampaignDetectionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CampaignDetectionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[CampaignDetection] Service started.");

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunDetectionAsync();
        }
    }

    private async Task RunDetectionAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ThreatCastDbContext>();
            var mlService = scope.ServiceProvider.GetRequiredService<MLService>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ThreatCastHub>>();

            var cutoff = DateTime.UtcNow.AddHours(-6);

            // Fetch approved events from the last 6 hours that are NOT yet part of a campaign
            var events = await db.AttackEvents
                .Include(e => e.Location)
                .Include(e => e.AttackReport)
                .Where(e => e.OccurredAt >= cutoff && e.CampaignId == null)
                .ToListAsync();

            if (events.Count < 10) return; // Need at least 10 events to check for campaign patterns

            // Group events by Class C IP Range (e.g. 192.168.1.0/24)
            var ipGroups = events
                .Where(e => e.AttackReport != null && !string.IsNullOrEmpty(e.AttackReport.SourceIP))
                .GroupBy(e => GetClassCSubnet(e.AttackReport!.SourceIP))
                .Where(g => g.Count() >= 10) // Minimum 10 events per subnet
                .ToList();

            foreach (var group in ipGroups)
            {
                var groupEvents = group.ToList();

                // Map events to ML service inputs
                var mlInputs = groupEvents.Select(e => new AttackEventInput
                {
                    AttackType = e.AttackType.ToString(),
                    AnomalyScore = e.Severity * 20.0,
                    PacketLength = 500,
                    Protocol = "TCP",
                    GeoLocation = e.Location?.CityName ?? "Unknown",
                    NetworkSegment = "Enterprise"
                }).ToList();

                // Query ML service
                var mlResult = await mlService.DetectCampaignAsync(mlInputs);

                if (mlResult != null && mlResult.IsCampaign)
                {
                    // Assign Alert Level based on event counts
                    AlertLevel alertLevel = groupEvents.Count switch
                    {
                        >= 50 => AlertLevel.Critical,
                        >= 20 => AlertLevel.High,
                        _ => AlertLevel.Medium
                    };

                    var campaign = new ThreatCampaign
                    {
                        Id = Guid.NewGuid(),
                        IpRange = group.Key,
                        DetectedAt = DateTime.UtcNow,
                        AffectedCities = string.Join(", ", groupEvents.Select(e => e.Location?.CityName ?? "Unknown").Distinct()),
                        AffectedSectors = string.Join(", ", groupEvents.Select(e => e.TargetSector.ToString()).Distinct()),
                        ReportCount = groupEvents.Count,
                        AlertLevel = alertLevel
                    };

                    db.ThreatCampaigns.Add(campaign);

                    // Associate events with the campaign
                    foreach (var ev in groupEvents)
                    {
                        ev.CampaignId = campaign.Id;
                    }

                    // Audit Log entry
                    db.AuditLogs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        Action = "MLDetectCampaign",
                        TargetEntity = "ThreatCampaign",
                        TargetEntityId = campaign.Id,
                        Reason = $"ML campaign isolated for IP subnet: {campaign.IpRange}. Alert level: {alertLevel}",
                        PerformedAt = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync();

                    _logger.LogInformation("[CampaignDetection] Campaign registered: {Subnet} ({Level})", campaign.IpRange, alertLevel);

                    // Broadcast via SignalR to Blazor map clients
                    await hubContext.Clients.Group("all_viewers").SendAsync("NewThreatCampaign", new
                    {
                        id = campaign.Id,
                        ipRange = campaign.IpRange,
                        detectedAt = campaign.DetectedAt,
                        affectedCities = campaign.AffectedCities,
                        affectedSectors = campaign.AffectedSectors,
                        reportCount = campaign.ReportCount,
                        alertLevel = campaign.AlertLevel.ToString()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CampaignDetection] Error running campaign detection job.");
        }
    }

    private static string GetClassCSubnet(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return "Unknown";
        var parts = ip.Split('.');
        return parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}.0/24" : ip;
    }
}