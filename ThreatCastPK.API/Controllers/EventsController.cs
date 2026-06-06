using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreatCastPK.Database.Context;

namespace ThreatCastPK.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventsController(AppDbContext context)
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
            .Where(r => r.CreatedAt >= cutoff && r.IsApproved);

        if (!string.IsNullOrEmpty(city))
            query = query.Where(r => r.City == city);

        if (!string.IsNullOrEmpty(attackType))
            query = query.Where(r => r.AttackType == attackType);

        if (!string.IsNullOrEmpty(sector))
            query = query.Where(r => r.Sector == sector);

        if (severity.HasValue)
            query = query.Where(r => r.Severity == severity);

        var events = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.City,
                r.Latitude,
                r.Longitude,
                r.AttackType,
                r.Sector,
                r.Severity,
                r.ConfidenceTier,
                r.CreatedAt,
                r.Description
            })
            .ToListAsync();

        return Ok(events);
    }
}