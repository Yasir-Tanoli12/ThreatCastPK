// ThreatCastPK.API/BackgroundServices/NotificationDispatchService.cs
// Runs as an IHostedService.
// Listens for new AttackEvents via a shared channel and dispatches
// notifications to users whose AlertSubscriptions match the event.
// This approach avoids polling the database — controllers push events
// directly into the channel when they create an AttackEvent.

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using ThreatCastPK.API.Hubs;
using ThreatCastPK.API.Services;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.BackgroundServices;

// ── Payload passed from controllers into the channel ──
public record AttackEventNotificationPayload(
    Guid EventId,
    string AttackType,   // enum string e.g. "DDoS"
    string TargetSector, // enum string e.g. "Banking"
    string City,
    int Severity
);

// ── Singleton channel — controllers resolve this and write to it ──
public class NotificationChannel
{
    private readonly Channel<AttackEventNotificationPayload> _channel =
        Channel.CreateUnbounded<AttackEventNotificationPayload>(
            new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<AttackEventNotificationPayload> Writer => _channel.Writer;
    public ChannelReader<AttackEventNotificationPayload> Reader => _channel.Reader;
}

// ── Background service ──
public class NotificationDispatchService : BackgroundService
{
    private readonly NotificationChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ThreatCastHub> _hubContext;
    private readonly ILogger<NotificationDispatchService> _logger;


    public NotificationDispatchService(
    NotificationChannel channel,
    IServiceScopeFactory scopeFactory,
    IHubContext<ThreatCastHub> hubContext,
    ILogger<NotificationDispatchService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }   

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[NotificationDispatch] Service started.");

        await foreach (var payload in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DispatchAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[NotificationDispatch] Error dispatching for event {EventId}",
                    payload.EventId);
            }
        }
    }

    private async Task DispatchAsync(AttackEventNotificationPayload payload)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThreatCastDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Load active subscriptions with their owner's email
        var subscriptions = await db.AlertSubscriptions
            .Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();

        var notificationsToAdd = new List<Notification>();
        var userIdsToNotify = new HashSet<Guid>();

        foreach (var sub in subscriptions)
        {
            if (!MatchesSubscription(sub, payload)) continue;

            notificationsToAdd.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = sub.UserId,
                SubscriptionId = sub.Id,
                Message = BuildMessage(payload),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            userIdsToNotify.Add(sub.UserId);
        }

        if (notificationsToAdd.Count == 0) return;

        db.Notifications.AddRange(notificationsToAdd);
        await db.SaveChangesAsync();

        // Group by user and notify
        foreach (var userId in userIdsToNotify)
        {
            var userNotifs = notificationsToAdd
                .Where(n => n.UserId == userId)
                .ToList();

            var user = subscriptions
                .First(s => s.UserId == userId).User;

            // Push in-app via SignalR
            foreach (var notif in userNotifs)
            {
                await _hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync("NewNotification", new
                    {
                        id = notif.Id,
                        message = notif.Message,
                        createdAt = notif.CreatedAt,
                        notificationType = "AttackEvent"
                    });
            }

            // Send email notification
            _ = Task.Run(async () =>
            {
                try
                {
                    var emailHtml = $"""
                    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                        <div style="background: #0f172a; padding: 24px; border-radius: 8px 8px 0 0;">
                            <h1 style="color: #22d3ee; margin: 0;">ThreatCast PK</h1>
                            <p style="color: #94a3b8; margin: 4px 0 0;">Live Cyberattack Intelligence for Pakistan</p>
                        </div>
                        <div style="background: #1e293b; padding: 32px; border-radius: 0 0 8px 8px;">
                            <h2 style="color: #f1f5f9;">⚠ Threat Alert</h2>
                            <p style="color: #94a3b8;">Hi {user.Username}, a new threat matching your subscription was detected.</p>
                            <div style="background: #0f172a; border-left: 4px solid #22d3ee; padding: 16px; margin: 20px 0; border-radius: 4px;">
                                <p style="color: #f1f5f9; font-family: monospace; font-size: 14px; margin: 0;">
                                    {userNotifs.First().Message}
                                </p>
                            </div>
                            <p style="color: #64748b; font-size: 13px;">Time: {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC</p>
                            <a href="https://threatcastpk-web.azurewebsites.net/notifications"
                               style="display: inline-block; background: #22d3ee; color: #0f172a;
                                      padding: 12px 28px; border-radius: 6px; text-decoration: none;
                                      font-weight: bold; margin: 16px 0;">
                                View All Notifications
                            </a>
                            <p style="color: #64748b; font-size: 12px;">
                                To manage your alert subscriptions, visit
                                <a href="https://threatcastpk-web.azurewebsites.net/subscriptions" style="color: #22d3ee;">
                                    your subscriptions page
                                </a>.
                            </p>
                        </div>
                    </div>
                    """;

                    await emailService.SendAsync(
                        user.Email,
                        $"ThreatCast PK Alert — {payload.AttackType} in {payload.City}",
                        emailHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[NotificationDispatch] Failed to send email to user {UserId}", userId);
                }
            });
        }

        _logger.LogInformation(
            "[NotificationDispatch] Dispatched {Count} notifications for event {EventId}",
            notificationsToAdd.Count, payload.EventId);
    }

    // ── Subscription matching logic ──
    // All specified filters must match (AND logic).
    // Empty/null filter = match everything (wildcard).
    private static bool MatchesSubscription(
        AlertSubscription sub,
        AttackEventNotificationPayload payload)
    {
        // Severity check — always enforced
        if (payload.Severity < sub.MinimumSeverity)
            return false;

        // Attack type check
        if (!string.IsNullOrWhiteSpace(sub.AttackTypes))
        {
            var types = sub.AttackTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (types.Count > 0 && !types.Contains(payload.AttackType))
                return false;
        }

        // City check
        if (!string.IsNullOrWhiteSpace(sub.Cities))
        {
            var cities = sub.Cities
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (cities.Count > 0 && !cities.Contains(payload.City))
                return false;
        }

        // Sector check
        if (!string.IsNullOrWhiteSpace(sub.Sectors))
        {
            var sectors = sub.Sectors
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (sectors.Count > 0 && !sectors.Contains(payload.TargetSector))
                return false;
        }

        return true;
    }

    private static string BuildMessage(AttackEventNotificationPayload p)
        => $"New {p.AttackType} attack in {p.City} — {p.TargetSector} sector — Severity {p.Severity}";
}