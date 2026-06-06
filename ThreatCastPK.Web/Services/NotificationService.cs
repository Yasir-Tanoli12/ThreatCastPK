// ThreatCastPK.Web/Services/NotificationService.cs
// Holds in-memory notifications received via SignalR.
// NavBar subscribes to OnChange to update the bell badge count.
// The Notifications page subscribes to OnChange to update its list.

namespace ThreatCastPK.Web.Services;

public class InAppNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}

public class NotificationService
{
    private readonly List<InAppNotification> _notifications = new();

    // NavBar and Notifications page subscribe to this
    public event Action? OnChange;

    // How many unread — displayed on the bell badge
    public int UnreadCount => _notifications.Count(n => !n.IsRead);

    // Full list — used by the Notifications page
    public IReadOnlyList<InAppNotification> All =>
        _notifications.OrderByDescending(n => n.ReceivedAt).ToList();

    // Called by SignalRService when "NewNotification" fires
    public void AddNotification(string message)
    {
        _notifications.Add(new InAppNotification
        {
            Message = message,
            ReceivedAt = DateTime.UtcNow,
            IsRead = false
        });

        // Keep max 100 in memory — drop oldest read ones first
        if (_notifications.Count > 100)
        {
            var oldest = _notifications
                .Where(n => n.IsRead)
                .OrderBy(n => n.ReceivedAt)
                .FirstOrDefault();

            if (oldest != null)
                _notifications.Remove(oldest);
        }

        // Tell NavBar and Notifications page to re-render
        OnChange?.Invoke();
    }

    public void MarkAsRead(Guid id)
    {
        var n = _notifications.FirstOrDefault(x => x.Id == id);
        if (n != null)
        {
            n.IsRead = true;
            OnChange?.Invoke();
        }
    }

    public void MarkAllAsRead()
    {
        foreach (var n in _notifications)
            n.IsRead = true;
        OnChange?.Invoke();
    }

    public void ClearAll()
    {
        _notifications.Clear();
        OnChange?.Invoke();
    }
}