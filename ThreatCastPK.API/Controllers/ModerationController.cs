using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
<<<<<<< HEAD
=======
using ThreatCastPK.API.BackgroundServices;
>>>>>>> haadi-cyber
using ThreatCastPK.API.DTOs;
using ThreatCastPK.API.Hubs;
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
        private readonly IHubContext<ThreatCastHub> _hubContext;
<<<<<<< HEAD

        public ModerationController(ThreatCastDbContext context, IHubContext<ThreatCastHub> hubContext)
        {
=======
        private readonly NotificationChannel _notificationChannel;

        public ModerationController(ThreatCastDbContext context, IHubContext<ThreatCastHub> hubContext, NotificationChannel notificationChannel)
        {
            _notificationChannel = notificationChannel;
>>>>>>> haadi-cyber
            _context = context;
            _hubContext = hubContext;
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
<<<<<<< HEAD
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
=======
                .FirstOrDefaultAsync(l => EF.Functions.ILike(l.CityName, report.City));

            if (location == null)
                return BadRequest(new { message = "City not recognized. Please seed locations before approving." });
>>>>>>> haadi-cyber

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

<<<<<<< HEAD
=======
            var notificationsToSend = await BuildNotificationsAsync(
                attackEvent,
                report.City,
                report.AttackType,
                report.TargetSector,
                report.Severity);

            _context.Notifications.AddRange(notificationsToSend.Select(n => n.Notification));

>>>>>>> haadi-cyber
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
<<<<<<< HEAD
=======
            await _notificationChannel.Writer.WriteAsync(new AttackEventNotificationPayload(
     EventId: attackEvent.Id,
     AttackType: attackEvent.AttackType.ToString(),
     TargetSector: attackEvent.TargetSector.ToString(),
     City: location.CityName,
     Severity: attackEvent.Severity
 ));

            foreach (var notification in notificationsToSend)
            {
                await _hubContext.Clients.Group($"user_{notification.UserId}")
                    .SendAsync("NewNotification", new
                    {
                        id = notification.Notification.Id,
                        message = notification.Notification.Message,
                        createdAt = notification.Notification.CreatedAt,
                        notificationType = notification.Notification.NotificationType
                    });
            }
>>>>>>> haadi-cyber

            // Broadcast new event to all connected clients via SignalR
            await _hubContext.Clients.Group("all_viewers").SendAsync("NewAttackEvent", new
            {
                id = attackEvent.Id,
                city = report.City,
                attackType = report.AttackType.ToString(),
                targetSector = report.TargetSector.ToString(),
                severity = report.Severity,
                occurredAt = attackEvent.OccurredAt,
                confidenceTier = "CommunityReported",
                source = "Community"
            });

            return Ok(new { message = "Report approved. Attack event created and broadcast to map." });
        }

<<<<<<< HEAD
=======
        private async Task<List<(Guid UserId, Notification Notification)>> BuildNotificationsAsync(
            AttackEvent attackEvent,
            string city,
            AttackType attackType,
            Sector sector,
            int severity)
        {
            var subscriptions = await _context.AlertSubscriptions
                .Where(s => s.IsActive && s.MinimumSeverity <= severity)
                .ToListAsync();

            var notifications = new List<(Guid UserId, Notification Notification)>();
            var attackTypeValue = attackType.ToString();
            var sectorValue = sector.ToString();

            foreach (var subscription in subscriptions)
            {
                var attackTypes = ParseList(subscription.AttackTypes);
                var cities = ParseList(subscription.Cities);
                var sectors = ParseList(subscription.Sectors);

                if (attackTypes.Count > 0 && !attackTypes.Contains(attackTypeValue))
                    continue;

                if (cities.Count > 0 && !cities.Contains(city))
                    continue;

                if (sectors.Count > 0 && !sectors.Contains(sectorValue))
                    continue;

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = subscription.UserId,
                    SubscriptionId = subscription.Id,
                    Message = $"New {attackTypeValue} attack in {city} targeting {sectorValue} (Severity {severity}).",
                    NotificationType = "AttackEvent",
                    CreatedAt = DateTime.UtcNow
                };

                notifications.Add((subscription.UserId, notification));
            }

            return notifications;
        }

        private static HashSet<string> ParseList(string value)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

>>>>>>> haadi-cyber
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

        // CRUD 3 — Get All Users
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