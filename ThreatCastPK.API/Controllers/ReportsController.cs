using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.API.Hubs;
using ThreatCastPK.API.Services;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ThreatCastDbContext _context;
        private readonly AbuseIPDBService _abuseIPDB;
        private readonly IHubContext<ThreatCastHub> _hubContext;

        public ReportsController(
            ThreatCastDbContext context,
            AbuseIPDBService abuseIPDB,
            IHubContext<ThreatCastHub> hubContext)
        {
            _context = context;
            _abuseIPDB = abuseIPDB;
            _hubContext = hubContext;
        }

        // Submit Attack Report
        [HttpPost]
        [Authorize(Roles = "Reporter,Admin")]
        public async Task<IActionResult> SubmitReport([FromBody] SubmitReportDTO dto)
        {
            var reporterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var reporter = await _context.Users.FindAsync(reporterId);

            if (reporter == null)
                return NotFound(new { message = "User not found." });

            if (reporter.IsSuspended)
                return Unauthorized(new { message = "Your account is suspended." });

            // Validate severity
            if (dto.Severity < 1 || dto.Severity > 5)
                return BadRequest(new { message = "Severity must be between 1 and 5." });

            // Validate attack type
            if (!Enum.TryParse<AttackType>(dto.AttackType, true, out var attackType))
                return BadRequest(new { message = "Invalid attack type." });

            // Validate sector
            if (!Enum.TryParse<Sector>(dto.TargetSector, true, out var sector))
                return BadRequest(new { message = "Invalid target sector." });

            // Check for duplicate within 24 hours
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var duplicate = await _context.AttackReports
                .AnyAsync(r => r.City == dto.City
                            && r.AttackType == attackType
                            && r.SubmittedAt >= cutoff
                            && !r.IsDeleted);

            if (duplicate)
                return Conflict(new { message = "This attack has already been reported in the last 24 hours." });

            // Find or create location
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.CityName == dto.City);

            if (location == null)
            {
                location = new Location
                {
                    Id = Guid.NewGuid(),
                    CityName = dto.City,
                    Province = "Unknown",
                    Latitude = 0,
                    Longitude = 0
                };
                _context.Locations.Add(location);
            }

            // Check IP reputation if source IP provided
            int abuseScore = 0;
            if (!string.IsNullOrEmpty(dto.SourceIP))
                abuseScore = await _abuseIPDB.GetAbuseConfidenceScore(dto.SourceIP);

            // Determine auto-approval
            bool autoApproved = reporter.ReputationScore >= 75 && abuseScore >= 80;

            var report = new AttackReport
            {
                Id = Guid.NewGuid(),
                ReporterId = reporterId,
                LocationId = location.Id,
                AttackType = attackType,
                TargetSector = sector,
                City = dto.City,
                Severity = dto.Severity,
                Description = dto.Description,
                SourceIP = dto.SourceIP,
                SubmittedAt = DateTime.UtcNow,
                Status = autoApproved ? ReportStatus.Approved : ReportStatus.Pending,
                ConfidenceTier = autoApproved ? ConfidenceTier.CommunityReported : ConfidenceTier.Unverified
            };

            _context.AttackReports.Add(report);

            if (autoApproved)
            {
                // Create attack event immediately
                var attackEvent = new AttackEvent
                {
                    Id = Guid.NewGuid(),
                    LocationId = location.Id,
                    ReportId = report.Id,
                    AttackType = attackType,
                    TargetSector = sector,
                    Severity = dto.Severity,
                    OccurredAt = DateTime.UtcNow,
                    ConfidenceTier = ConfidenceTier.CommunityReported,
                    Source = EventSource.Community
                };
                _context.AttackEvents.Add(attackEvent);

                // Increment reputation
                reporter.ReputationScore += 10;

                await _context.SaveChangesAsync();

                // Broadcast to all connected clients
                await _hubContext.Clients.Group("all_viewers").SendAsync("NewAttackEvent", new
                {
                    id = attackEvent.Id,
                    city = dto.City,
                    attackType = attackType.ToString(),
                    targetSector = sector.ToString(),
                    severity = dto.Severity,
                    occurredAt = attackEvent.OccurredAt,
                    confidenceTier = "CommunityReported",
                    source = "Community"
                });

                return Ok(new { message = "Report auto-approved and live on the map.", status = "Approved" });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Report submitted and pending admin review.", status = "Pending" });
        }

        // Get my reports
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReports([FromQuery] string? status)
        {
            var reporterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var query = _context.AttackReports
                .Where(r => r.ReporterId == reporterId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReportStatus>(status, true, out var reportStatus))
                query = query.Where(r => r.Status == reportStatus);

            var reports = await query
                .OrderByDescending(r => r.SubmittedAt)
                .Select(r => new ReportResponseDTO
                {
                    Id = r.Id,
                    AttackType = r.AttackType.ToString(),
                    TargetSector = r.TargetSector.ToString(),
                    City = r.City,
                    Severity = r.Severity,
                    Description = r.Description,
                    SourceIP = r.SourceIP,
                    SubmittedAt = r.SubmittedAt,
                    Status = r.Status.ToString(),
                    ConfidenceTier = r.ConfidenceTier.ToString(),
                    RejectionReason = r.RejectionReason
                })
                .ToListAsync();

            return Ok(reports);
        }
    }
}