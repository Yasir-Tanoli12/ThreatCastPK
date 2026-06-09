// ThreatCastPK.API/Controllers/AnalyticsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.API.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly ThreatCastDbContext _context;

    public AnalyticsController(ThreatCastDbContext context)
    {
        _context = context;
    }

    // GET /api/analytics/stats
    // Public — no auth required
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var todayStart = DateTime.UtcNow.Date;

        var todayEvents = await _context.AttackEvents
            .Where(e => e.OccurredAt >= todayStart)
            .ToListAsync();

        var topCity = await _context.AttackEvents
            .Include(e => e.Location)
            .Where(e => e.OccurredAt >= todayStart)
            .GroupBy(e => e.Location.CityName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        var topType = await _context.AttackEvents
            .Where(e => e.OccurredAt >= todayStart)
            .GroupBy(e => e.AttackType)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key.ToString())
            .FirstOrDefaultAsync();

        var topSector = await _context.AttackEvents
            .Where(e => e.OccurredAt >= todayStart)
            .GroupBy(e => e.TargetSector)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key.ToString())
            .FirstOrDefaultAsync();

        var totalAllTime = await _context.AttackEvents.CountAsync();

        return Ok(new StatsResponseDTO
        {
            TotalToday = todayEvents.Count,
            TopCity = topCity ?? "N/A",
            TopAttackType = topType ?? "N/A",
            TopSector = topSector ?? "N/A",
            TotalAllTime = totalAllTime
        });
    }

    // GET /api/analytics/by-city
    // Returns attack counts grouped by city for last 7 days
    [HttpGet("by-city")]
    public async Task<IActionResult> GetByCity()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var data = await _context.AttackEvents
            .Include(e => e.Location)
            .Where(e => e.OccurredAt >= cutoff)
            .GroupBy(e => e.Location.CityName)
            .Select(g => new CityCountDTO
            {
                City = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return Ok(data);
    }

    // GET /api/analytics/by-type
    // Returns attack counts grouped by attack type (all time)
    [HttpGet("by-type")]
    public async Task<IActionResult> GetByType()
    {
        var data = await _context.AttackEvents
            .GroupBy(e => e.AttackType)
            .Select(g => new TypeCountDTO
            {
                AttackType = g.Key.ToString(),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return Ok(data);
    }

    // GET /api/analytics/trend
    // Returns daily event count for last 30 days
    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend()
    {
        var cutoff = DateTime.UtcNow.AddDays(-29).Date;

        var events = await _context.AttackEvents
            .Where(e => e.OccurredAt >= cutoff)
            .ToListAsync();

        // Build all 30 days even if some have zero events
        var trend = Enumerable.Range(0, 30)
            .Select(i =>
            {
                var date = cutoff.AddDays(i);
                return new TrendPointDTO
                {
                    Date = date.ToString("MMM d"),
                    Count = events.Count(e => e.OccurredAt.Date == date)
                };
            })
            .ToList();

        return Ok(trend);
    }

    // GET /api/analytics/sector-risk
    // Returns risk level per sector based on last 24h event counts
    [HttpGet("sector-risk")]
    public async Task<IActionResult> GetSectorRisk()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var sectors = Enum.GetValues<Sector>().Cast<Sector>().ToList();

        var events = await _context.AttackEvents
            .Where(e => e.OccurredAt >= cutoff)
            .ToListAsync();

        var result = sectors.Select(sector =>
        {
            var count = events.Count(e => e.TargetSector == sector);
            var hasCritical = events.Any(e =>
                e.TargetSector == sector && e.Severity >= 5);

            var risk = (count, hasCritical) switch
            {
                (_, true) => "Critical",
                ( >= 21, _) => "Critical",
                ( >= 6, _) => "High",
                ( >= 1, _) => "Medium",
                _ => "Low"
            };

            return new SectorRiskDTO
            {
                Sector = sector.ToString(),
                RiskLevel = risk,
                EventCount = count
            };
        }).ToList();

        return Ok(result);
    }

    // GET /api/analytics/recent-events
    // Returns high severity events (4+) from last 24h
    [HttpGet("recent-events")]
    public async Task<IActionResult> GetRecentEvents()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var events = await _context.AttackEvents
            .Include(e => e.Location)
            .Where(e => e.OccurredAt >= cutoff && e.Severity >= 4)
            .OrderByDescending(e => e.OccurredAt)
            .Take(20)
            .ToListAsync();

        var result = events.Select(e => new RecentEventDTO
        {
            Time = e.OccurredAt.ToString("HH:mm"),
            AttackType = e.AttackType.ToString(),
            City = e.Location?.CityName ?? "Unknown",
            TargetSector = e.TargetSector.ToString(),
            Severity = e.Severity,
            Source = e.Source.ToString()
        }).ToList();

        return Ok(result);
    }
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] string timeFilter = "24h")
    {
        var cutoff = timeFilter switch
        {
            "1h" => DateTime.UtcNow.AddHours(-1),
            "6h" => DateTime.UtcNow.AddHours(-6),
            "7d" => DateTime.UtcNow.AddDays(-7),
            _ => DateTime.UtcNow.AddHours(-24)
        };

        var events = await _context.AttackEvents
            .Include(e => e.Location)
            .Where(e => e.OccurredAt >= cutoff)
            .OrderByDescending(e => e.OccurredAt)
            .Take(200)
            .Select(e => new
            {
                id = e.Id,
                attackType = e.AttackType.ToString(),
                city = e.Location != null ? e.Location.CityName : "Unknown",
                targetSector = e.TargetSector.ToString(),
                severity = e.Severity,
                occurredAt = e.OccurredAt,
                latitude = e.Location != null ? e.Location.Latitude : 0.0,
                longitude = e.Location != null ? e.Location.Longitude : 0.0,
                source = e.Source.ToString()
            })
            .ToListAsync();

        return Ok(events);
    }
    // GET /api/analytics/campaigns
    // Returns active threat campaigns from the last 24 hours
    [HttpGet("campaigns")]
    public async Task<IActionResult> GetActiveCampaigns()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var campaigns = await _context.ThreatCampaigns
            .Where(c => c.DetectedAt >= cutoff)
            .OrderByDescending(c => c.DetectedAt)
            .Select(c => new
            {
                id = c.Id,
                ipRange = c.IpRange,
                detectedAt = c.DetectedAt,
                affectedCities = c.AffectedCities,
                affectedSectors = c.AffectedSectors,
                reportCount = c.ReportCount,
                alertLevel = c.AlertLevel.ToString()
            })
            .ToListAsync();

        return Ok(campaigns);
    }
   

}