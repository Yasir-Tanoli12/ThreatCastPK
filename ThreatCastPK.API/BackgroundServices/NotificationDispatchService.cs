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

        // Load all active subscriptions with their owner
        var subscriptions = await db.AlertSubscriptions
            .Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();

        var notificationsToAdd = new List<Notification>();
        var userIdsToNotify = new HashSet<Guid>();

        foreach (var sub in subscriptions)
        {
            if (!MatchesSubscription(sub, payload)) continue;

            var message = BuildMessage(payload);

            notificationsToAdd.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = sub.UserId,
                SubscriptionId = sub.Id,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            userIdsToNotify.Add(sub.UserId);
        }

        if (notificationsToAdd.Count == 0) return;

        db.Notifications.AddRange(notificationsToAdd);
        await db.SaveChangesAsync();

        // Push via SignalR to each user's personal group
        foreach (var userId in userIdsToNotify)
        {
            var userNotifs = notificationsToAdd
                .Where(n => n.UserId == userId)
                .ToList();

            foreach (var notif in userNotifs)
            {
                await _hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync("NewNotification", notif.Message);
            }
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