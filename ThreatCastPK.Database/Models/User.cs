using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.Database.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public string? GoogleId { get; set; }
        public UserRole Role { get; set; } = UserRole.Registered;
        public int ReputationScore { get; set; } = 0;
        public bool IsSuspended { get; set; } = false;
        public bool ReporterRequestPending { get; set; } = false;
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<AttackReport> AttackReports { get; set; } = new List<AttackReport>();
        public ICollection<AlertSubscription> AlertSubscriptions { get; set; } = new List<AlertSubscription>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<DiscussionPost> DiscussionPosts { get; set; } = new List<DiscussionPost>();
        public ICollection<ThreatAdvisory> ThreatAdvisories { get; set; } = new List<ThreatAdvisory>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}