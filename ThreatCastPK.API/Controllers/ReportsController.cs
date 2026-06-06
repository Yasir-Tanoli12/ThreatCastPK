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
<<<<<<< HEAD

        public ReportsController(
            ThreatCastDbContext context,
            AbuseIPDBService abuseIPDB,
            IHubContext<ThreatCastHub> hubContext)
        {
=======
        private readonly NotificationChannel _notificationChannel;

        public ReportsController(ThreatCastDbContext context,
            AbuseIPDBService abuseIPDB,
            IHubContext<ThreatCastHub> hubContext,
            NotificationChannel notificationChannel)
        {
            _notificationChannel = notificationChannel;     
>>>>>>> haadi-cyber
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

<<<<<<< HEAD
=======
            // Rate limit: max 10 submissions per reporter per hour
            var rateCutoff = DateTime.UtcNow.AddHours(-1);
            var recentReportsCount = await _context.AttackReports
                .CountAsync(r => r.ReporterId == reporterId
                              && r.SubmittedAt >= rateCutoff
                              && !r.IsDeleted);

            if (recentReportsCount >= 10)
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = "Rate limit exceeded. Max 10 reports per hour." });

>>>>>>> haadi-cyber
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
<<<<<<< HEAD
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
=======
                .FirstOrDefaultAsync(l => EF.Functions.ILike(l.CityName, dto.City));

            if (location == null)
                return BadRequest(new { message = "City not recognized. Please choose a supported city." });
>>>>>>> haadi-cyber

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

<<<<<<< HEAD
                await _context.SaveChangesAsync();
=======
                var notificationsToSend = await BuildNotificationsAsync(
                    attackEvent,
                    dto.City,
                    attackType,
                    sector,
                    dto.Severity);

                _context.Notifications.AddRange(notificationsToSend.Select(n => n.Notification));

                await _context.SaveChangesAsync();
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