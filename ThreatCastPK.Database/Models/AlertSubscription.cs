using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.Database.Models
{
    public class AlertSubscription
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string AttackTypes { get; set; } = string.Empty;
        public string Cities { get; set; } = string.Empty;
        public string Sectors { get; set; } = string.Empty;
        public int MinimumSeverity { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}