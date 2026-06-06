// ThreatCastPK.API/BackgroundServices/SectorRiskScoringService.cs
// Runs every 30 minutes.
// For each sector, counts AttackEvents in the last 24 hours,
// checks for any Critical severity events, then writes a
// risk level to the SectorRiskScores table.
// The Analytics endpoint reads from this table.

using Microsoft.EntityFrameworkCore;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.BackgroundServices;

public class SectorRiskScoringService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SectorRiskScoringService> _logger;

    // Run every 30 minutes
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    public SectorRiskScoringService(
        IServiceScopeFactory scopeFactory,
        ILogger<SectorRiskScoringService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SectorRiskScoring] Service started.");

        // Run once immediately on startup to populate the table
        await RunScoringAsync();

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunScoringAsync();
        }
    }

    private async Task RunScoringAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ThreatCastDbContext>();

            var cutoff = DateTime.UtcNow.AddHours(-24);

            // Load all recent events once — avoid N+1 queries
            var recentEvents = await db.AttackEvents
                .Where(e => e.OccurredAt >= cutoff)
                .ToListAsync();

            var sectors = Enum.GetValues<Sector>().Cast<Sector>().ToList();

            foreach (var sector in sectors)
            {
                var sectorEvents = recentEvents
                    .Where(e => e.TargetSector == sector)
                    .ToList();

                var count = sectorEvents.Count;
                var hasCritical = sectorEvents.Any(e => e.Severity >= 5);

                var riskLevel = (count, hasCritical) switch
                {
                    (_, true) => RiskLevel.Critical,
                    ( >= 21, _) => RiskLevel.Critical,
                    ( >= 6, _) => RiskLevel.High,
                    ( >= 1, _) => RiskLevel.Medium,
                    _ => RiskLevel.Low
                };

                // Upsert — update if exists, insert if not
                var existing = await db.SectorRiskScores
                    .FirstOrDefaultAsync(s => s.SectorName == sector.ToString());

                if (existing == null)
                {
                    db.SectorRiskScores.Add(new SectorRiskScore
                    {
                        Id = Guid.NewGuid(),
                        SectorName = sector.ToString(),
                        RiskLevel = riskLevel,
                        EventCount24h = count,
                        LastCalculatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.RiskLevel = riskLevel;
                    existing.EventCount24h = count;
                    existing.LastCalculatedAt = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();

            _logger.LogInformation(
                "[SectorRiskScoring] Recalculated risk scores for {Count} sectors at {Time}",
                sectors.Count, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SectorRiskScoring] Error during risk score calculation.");
        }
    }
}