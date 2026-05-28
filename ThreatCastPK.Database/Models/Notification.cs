namespace ThreatCastPK.Database.Models
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string NotificationType { get; set; } = string.Empty;

        // Navigation properties
        public User User { get; set; } = null!;
        public AlertSubscription AlertSubscription { get; set; } = null!;
    }
}