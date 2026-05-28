namespace ThreatCastPK.Database.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public Guid TargetEntityId { get; set; }
        public string? Reason { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User Admin { get; set; } = null!;
    }
}