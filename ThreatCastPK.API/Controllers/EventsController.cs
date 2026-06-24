using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ThreatCastDbContext _context;

    public EventsController(ThreatCastDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(
    [FromQuery] string timeWindow = "24h",
    [FromQuery] string? city = null,
    [FromQuery] string? attackType = null,
    [FromQuery] string? sector = null,
    [FromQuery] int? severity = null)
    {
        var cutoff = timeWindow switch
        {
            "1h" => DateTime.UtcNow.AddHours(-1),
            "6h" => DateTime.UtcNow.AddHours(-6),
            "7d" => DateTime.UtcNow.AddDays(-7),
            _ => DateTime.UtcNow.AddHours(-24)
        };

        var query = _context.AttackReports
            .Include(r => r.AttackEvent)
            .Include(r => r.Location)
            .Where(r => r.SubmittedAt >= cutoff &&
                        r.Status == ReportStatus.Approved);

        if (!string.IsNullOrEmpty(city))
            query = query.Where(r => r.City == city);

        if (!string.IsNullOrEmpty(attackType) &&
            Enum.TryParse<AttackType>(attackType, out var parsedAttackType))
            query = query.Where(r => r.AttackType == parsedAttackType);

        if (!string.IsNullOrEmpty(sector) &&
            Enum.TryParse<Sector>(sector, out var parsedSector))
            query = query.Where(r => r.TargetSector == parsedSector);

        if (severity.HasValue)
            query = query.Where(r => r.Severity == severity.Value);

        var events = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new
            {
                r.Id,
                r.City,
                r.LocationId,
                lat = r.Location.Latitude,
                lng = r.Location.Longitude,
                r.AttackType,
                r.TargetSector,
                r.Severity,
                r.ConfidenceTier,
                r.SubmittedAt,
                r.Description,
                greyNoiseClassification = r.AttackEvent != null
                    ? r.AttackEvent.GreyNoiseClassification
                    : null
            })
            .ToListAsync();

        return Ok(events);
    }
}