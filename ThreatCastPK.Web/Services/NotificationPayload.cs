namespace ThreatCastPK.Web.Services;

public class NotificationPayload
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string NotificationType { get; set; } = string.Empty;
}