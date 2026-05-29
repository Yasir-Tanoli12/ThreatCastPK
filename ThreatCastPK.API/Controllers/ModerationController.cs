using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ModerationController : ControllerBase
    {
        private readonly ThreatCastDbContext _context;

        public ModerationController(ThreatCastDbContext context)
        {
            _context = context;
        }

        // CRUD 3 — Get Moderation Queue
        [HttpGet("queue")]
        public async Task<IActionResult> GetModerationQueue()
        {
            var reports = await _context.AttackReports
                .Where(r => r.Status == ReportStatus.Pending && !r.IsDeleted)
                .Include(r => r.Reporter)
                .OrderBy(r => r.SubmittedAt)
                .Select(r => new ModerationReportResponseDTO
                {
                    Id = r.Id,
                    ReporterUsername = r.Reporter.Username,
                    AttackType = r.AttackType.ToString(),
                    TargetSector = r.TargetSector.ToString(),
                    City = r.City,
                    Severity = r.Severity,
                    Description = r.Description,
                    SourceIP = r.SourceIP,
                    SubmittedAt = r.SubmittedAt,
                    Status = r.Status.ToString(),
                    ConfidenceTier = r.ConfidenceTier.ToString()
                })
                .ToListAsync();

            return Ok(reports);
        }

        // CRUD 3 — Approve Report
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveReport(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var report = await _context.AttackReports
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound(new { message = "Report not found." });

            if (report.Status != ReportStatus.Pending)
                return BadRequest(new { message = "Report is not pending." });

            // Find location or create one
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.CityName == report.City);

            if (location == null)
            {
                location = new Location
                {
                    Id = Guid.NewGuid(),
                    CityName = report.City,
                    Province = "Unknown",
                    Latitude = 0,
                    Longitude = 0
                };
                _context.Locations.Add(location);
            }

            // Update report status
            report.Status = ReportStatus.Approved;
            report.ConfidenceTier = ConfidenceTier.CommunityReported;

            // Create attack event
            var attackEvent = new AttackEvent
            {
                Id = Guid.NewGuid(),
                LocationId = location.Id,
                ReportId = report.Id,
                AttackType = report.AttackType,
                TargetSector = report.TargetSector,
                Severity = report.Severity,
                OccurredAt = report.SubmittedAt,
                ConfidenceTier = ConfidenceTier.CommunityReported,
                Source = EventSource.Community
            };
            _context.AttackEvents.Add(attackEvent);

            // Update reporter reputation
            report.Reporter.ReputationScore += 10;

            // Write audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "ApproveReport",
                TargetEntity = "AttackReport",
                TargetEntityId = report.Id,
                Reason = "Report approved by admin",
                PerformedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Report approved. Attack event created." });
        }

        // CRUD 3 — Reject Report
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectReport(Guid id, [FromBody] RejectReportDTO dto)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var report = await _context.AttackReports
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound(new { message = "Report not found." });

            if (report.Status != ReportStatus.Pending)
                return BadRequest(new { message = "Report is not pending." });

            if (string.IsNullOrEmpty(dto.Reason))
                return BadRequest(new { message = "Rejection reason is required." });

            // Soft delete and reject
            report.Status = ReportStatus.Rejected;
            report.RejectionReason = dto.Reason;
            report.IsDeleted = true;

            // Deduct reporter reputation
            report.Reporter.ReputationScore = Math.Max(0, report.Reporter.ReputationScore - 5);

            // Write audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "RejectReport",
                TargetEntity = "AttackReport",
                TargetEntityId = report.Id,
                Reason = dto.Reason,
                PerformedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Report rejected." });
        }

        // CRUD 3 — Get All Users (for user management)
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    Role = u.Role.ToString(),
                    u.ReputationScore,
                    u.IsSuspended,
                    u.ReporterRequestPending,
                    u.JoinDate
                })
                .ToListAsync();

            return Ok(users);
        }

        // CRUD 3 — Grant Reporter Status
        [HttpPut("users/{id}/grant-reporter")]
        public async Task<IActionResult> GrantReporter(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Role = UserRole.Reporter;
            user.ReporterRequestPending = false;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "GrantReporter",
                TargetEntity = "User",
                TargetEntityId = id,
                Reason = "Reporter status granted by admin",
                PerformedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Reporter status granted." });
        }

        // CRUD 3 — Revoke Reporter Status
        [HttpPut("users/{id}/revoke-reporter")]
        public async Task<IActionResult> RevokeReporter(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Role = UserRole.Registered;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "RevokeReporter",
                TargetEntity = "User",
                TargetEntityId = id,
                Reason = "Reporter status revoked by admin",
                PerformedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Reporter status revoked." });
        }

        // CRUD 3 — Suspend User
        [HttpPut("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id, [FromBody] RejectReportDTO dto)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.IsSuspended)
                return BadRequest(new { message = "User is already suspended." });

            user.IsSuspended = true;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "SuspendUser",
                TargetEntity = "User",
                TargetEntityId = id,
                Reason = dto.Reason,
                PerformedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            return Ok(new { message = "User suspended." });
        }

        // CRUD 3 — Unsuspend User
        [HttpPut("users/{id}/unsuspend")]
        public async Task<IActionResult> UnsuspendUser(Guid id)
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.IsSuspended = false;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "UnsuspendUser",
                TargetEntity = "User",
                TargetEntityId = id,
                Reason = "User unsuspended by admin",
                PerformedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            return Ok(new { message = "User unsuspended." });
        }
    }
}