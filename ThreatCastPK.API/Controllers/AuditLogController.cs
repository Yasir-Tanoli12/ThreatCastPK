// ThreatCastPK.API/Controllers/AuditLogController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreatCastPK.Database.Context;

namespace ThreatCastPK.API.Controllers;

public class AuditLogResponseDTO
{
    public Guid Id { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
    public string? Reason { get; set; }
    public DateTime PerformedAt { get; set; }
}

[ApiController]
[Route("api/auditlog")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly ThreatCastDbContext _context;

    public AuditLogController(ThreatCastDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var query = _context.AuditLogs
            .Include(a => a.Admin)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponseDTO
            {
                Id = a.Id,
                AdminUsername = a.Admin.Username,
                Action = a.Action,
                TargetEntity = a.TargetEntity,
                TargetEntityId = a.TargetEntityId,
                Reason = a.Reason,
                PerformedAt = a.PerformedAt
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            logs
        });
    }

    [HttpGet("actions")]
    public async Task<IActionResult> GetActions()
    {
        var actions = await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return Ok(actions);
    }
}