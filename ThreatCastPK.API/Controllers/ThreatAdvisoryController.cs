// ThreatCastPK.API/Controllers/ThreatAdvisoryController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.Controllers;

[ApiController]
[Route("api/advisories")]
public class ThreatAdvisoryController : ControllerBase
{
    private readonly ThreatCastDbContext _context;

    public ThreatAdvisoryController(ThreatCastDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAdvisories(
        [FromQuery] bool includeArchived = false)
    {
        var query = _context.ThreatAdvisories
            .Include(a => a.Admin)
            .AsQueryable();

        if (!includeArchived)
            query = query.Where(a => !a.IsArchived);

        var advisories = await query
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                Body = a.Body,
                SeverityTag = a.SeverityTag,
                a.AffectedSectors,
                a.AffectedCities,
                AdminUsername = a.Admin.Username,
                PublishedAt = a.PublishedAt,
                a.IsArchived
            })
            .ToListAsync();

        return Ok(advisories);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAdvisory(Guid id)
    {
        var advisory = await _context.ThreatAdvisories
            .Include(a => a.Admin)
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.Title,
                Body = a.Body,
                SeverityTag = a.SeverityTag,
                a.AffectedSectors,
                a.AffectedCities,
                AdminUsername = a.Admin.Username,
                PublishedAt = a.PublishedAt,
                a.IsArchived
            })
            .FirstOrDefaultAsync();

        if (advisory == null)
            return NotFound(new { message = "Advisory not found." });

        return Ok(advisory);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAdvisory([FromBody] CreateAdvisoryDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Title is required." });

        if (string.IsNullOrWhiteSpace(dto.Body))
            return BadRequest(new { message = "Body is required." });

        var validSeverities = new[] { "Low", "Medium", "High", "Critical" };
        if (!validSeverities.Contains(dto.SeverityTag))
            return BadRequest(new { message = "SeverityTag must be Low, Medium, High, or Critical." });

        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var advisory = new ThreatAdvisory
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Body = dto.Body.Trim(),
            SeverityTag = dto.SeverityTag,
            AffectedSectors = dto.AffectedSectors?.Trim() ?? string.Empty,
            AffectedCities = dto.AffectedCities?.Trim() ?? string.Empty,
            AdminId = adminId,
            PublishedAt = DateTime.UtcNow,
            IsArchived = false
        };

        _context.ThreatAdvisories.Add(advisory);

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Action = "CreateAdvisory",
            TargetEntity = "ThreatAdvisory",
            TargetEntityId = advisory.Id,
            Reason = $"Created advisory: {dto.Title}",
            PerformedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAdvisory), new { id = advisory.Id },
            new { message = "Advisory published.", advisoryId = advisory.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAdvisory(Guid id, [FromBody] CreateAdvisoryDTO dto)
    {
        var advisory = await _context.ThreatAdvisories.FindAsync(id);

        if (advisory == null)
            return NotFound(new { message = "Advisory not found." });

        if (advisory.IsArchived)
            return BadRequest(new { message = "Cannot edit an archived advisory." });

        var validSeverities = new[] { "Low", "Medium", "High", "Critical" };
        if (!string.IsNullOrWhiteSpace(dto.SeverityTag)
            && !validSeverities.Contains(dto.SeverityTag))
            return BadRequest(new { message = "Invalid severity level." });

        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!string.IsNullOrWhiteSpace(dto.Title))
            advisory.Title = dto.Title.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Body))
            advisory.Body = dto.Body.Trim();
        if (!string.IsNullOrWhiteSpace(dto.SeverityTag))
            advisory.SeverityTag = dto.SeverityTag;
        if (dto.AffectedSectors != null)
            advisory.AffectedSectors = dto.AffectedSectors.Trim();
        if (dto.AffectedCities != null)
            advisory.AffectedCities = dto.AffectedCities.Trim();

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Action = "UpdateAdvisory",
            TargetEntity = "ThreatAdvisory",
            TargetEntityId = advisory.Id,
            Reason = $"Updated advisory: {advisory.Title}",
            PerformedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Advisory updated." });
    }

    [HttpPut("{id:guid}/archive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ArchiveAdvisory(Guid id)
    {
        var advisory = await _context.ThreatAdvisories.FindAsync(id);

        if (advisory == null)
            return NotFound(new { message = "Advisory not found." });

        if (advisory.IsArchived)
            return BadRequest(new { message = "Advisory is already archived." });

        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        advisory.IsArchived = true;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Action = "ArchiveAdvisory",
            TargetEntity = "ThreatAdvisory",
            TargetEntityId = advisory.Id,
            Reason = $"Archived advisory: {advisory.Title}",
            PerformedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Advisory archived." });
    }

    [HttpPut("{id:guid}/unarchive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnarchiveAdvisory(Guid id)
    {
        var advisory = await _context.ThreatAdvisories.FindAsync(id);

        if (advisory == null)
            return NotFound(new { message = "Advisory not found." });

        if (!advisory.IsArchived)
            return BadRequest(new { message = "Advisory is not archived." });

        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        advisory.IsArchived = false;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Action = "UnarchiveAdvisory",
            TargetEntity = "ThreatAdvisory",
            TargetEntityId = advisory.Id,
            Reason = $"Unarchived advisory: {advisory.Title}",
            PerformedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Advisory restored." });
    }
}