using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreatCastPK.API.BackgroundServices;
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
        private readonly GreyNoiseService _greyNoise;
        private readonly IHubContext<ThreatCastHub> _hubContext;
        private readonly NotificationChannel _notificationChannel;

        public ReportsController(
            ThreatCastDbContext context,
            AbuseIPDBService abuseIPDB,
            GreyNoiseService greyNoise,
            IHubContext<ThreatCastHub> hubContext,
            NotificationChannel notificationChannel)
        {
            _context = context;
            _abuseIPDB = abuseIPDB;
            _greyNoise = greyNoise;
            _hubContext = hubContext;
            _notificationChannel = notificationChannel;
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

            // Rate limit: max 10 reports/hour
            var cutoff = DateTime.UtcNow.AddHours(-1);
            var recentReportsCount = await _context.AttackReports
                .CountAsync(r => r.ReporterId == reporterId &&
                                 r.SubmittedAt >= cutoff &&
                                 !r.IsDeleted);

            if (recentReportsCount >= 10)
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = "Rate limit exceeded. Max 10 reports per hour." });

            // Validate severity
            if (dto.Severity < 1 || dto.Severity > 5)
                return BadRequest(new { message = "Severity must be between 1 and 5." });

            // Validate attack type
            if (!Enum.TryParse<AttackType>(dto.AttackType, true, out var attackType))
                return BadRequest(new { message = "Invalid attack type." });

            // Validate sector
            if (!Enum.TryParse<Sector>(dto.TargetSector, true, out var sector))
                return BadRequest(new { message = "Invalid target sector." });

            // Duplicate check (24h)
            var duplicateCutoff = DateTime.UtcNow.AddHours(-24);
            var duplicate = await _context.AttackReports
                .AnyAsync(r => r.City == dto.City &&
                               r.AttackType == attackType &&
                               r.SubmittedAt >= duplicateCutoff &&
                               !r.IsDeleted);

            if (duplicate)
                return Conflict(new { message = "This attack has already been reported in the last 24 hours." });

            // Find location
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => EF.Functions.ILike(l.CityName, dto.City));

            if (location == null)
                return BadRequest(new { message = "City not recognized. Please choose a supported city." });

            // Threat intelligence
            int abuseScore = 0;
            bool greyNoiseNoise = false;
            string? greyNoiseClassification = null;

            if (!string.IsNullOrEmpty(dto.SourceIP))
            {
                abuseScore = await _abuseIPDB.GetAbuseConfidenceScore(dto.SourceIP);
                var gnResult = await _greyNoise.ClassifyAsync(dto.SourceIP);
                greyNoiseNoise = gnResult.IsNoise;
                greyNoiseClassification = gnResult.Classification;
            }

            // Auto-approval logic
            bool autoApproved = reporter.ReputationScore >= 75 &&
                                abuseScore >= 80 &&
                                !greyNoiseNoise;

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
                var gnClassification = string.Empty;
                if (!string.IsNullOrEmpty(dto.SourceIP))
                {
                    var gnResult2 = await _greyNoise.ClassifyAsync(dto.SourceIP);
                    gnClassification = gnResult2.Classification;
                }

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
                    Source = EventSource.Community,
                    SourceIP = dto.SourceIP,
                    GreyNoiseClassification = greyNoiseClassification,
                };


                _context.AttackEvents.Add(attackEvent);

                reporter.ReputationScore += 10;

                var notificationsToSend = await BuildNotificationsAsync(
                    attackEvent,
                    dto.City,
                    attackType,
                    sector,
                    dto.Severity);


                await _context.SaveChangesAsync();

                await _notificationChannel.Writer.WriteAsync(
                    new AttackEventNotificationPayload(
                        EventId: attackEvent.Id,
                        AttackType: attackEvent.AttackType.ToString(),
                        TargetSector: attackEvent.TargetSector.ToString(),
                        City: location.CityName,
                        Severity: attackEvent.Severity
                    )
                );



                await _hubContext.Clients.Group("all_viewers")
    .SendAsync("NewAttackEvent", new
    {
        id = attackEvent.Id,
        city = dto.City,
        attackType = attackType.ToString(),
        targetSector = sector.ToString(),
        severity = dto.Severity,
        occurredAt = attackEvent.OccurredAt,
        confidenceTier = "CommunityReported",
        source = "Community",
        greyNoiseClassification = greyNoiseClassification
    });

                return Ok(new
                {
                    message = "Report auto-approved and live on the map.",
                    status = "Approved"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Report submitted and pending admin review.",
                status = "Pending"
            });
        }

        // Get my reports
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReports([FromQuery] string? status)
        {
            var reporterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var query = _context.AttackReports
                .Where(r => r.ReporterId == reporterId);

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<ReportStatus>(status, true, out var reportStatus))
            {
                query = query.Where(r => r.Status == reportStatus);
            }

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

        // Notification builder
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

            var notifications = new List<(Guid, Notification)>();

            var attackTypeValue = attackType.ToString();
            var sectorValue = sector.ToString();

            foreach (var sub in subscriptions)
            {
                var attackTypes = ParseList(sub.AttackTypes);
                var cities = ParseList(sub.Cities);
                var sectors = ParseList(sub.Sectors);

                if (attackTypes.Count > 0 && !attackTypes.Contains(attackTypeValue)) continue;
                if (cities.Count > 0 && !cities.Contains(city)) continue;
                if (sectors.Count > 0 && !sectors.Contains(sectorValue)) continue;

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = sub.UserId,
                    SubscriptionId = sub.Id,
                    Message = $"New {attackTypeValue} attack in {city} targeting {sectorValue} (Severity {severity}).",
                    NotificationType = "AttackEvent",
                    CreatedAt = DateTime.UtcNow
                };

                notifications.Add((sub.UserId, notification));
            }

            return notifications;
        }

        private static HashSet<string> ParseList(string value)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}