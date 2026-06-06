using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.Database.Models
{
    public class AttackReport
    {
        public Guid Id { get; set; }
        public Guid ReporterId { get; set; }
        public Guid LocationId { get; set; }
        public AttackType AttackType { get; set; }
        public Sector TargetSector { get; set; }
        public string City { get; set; } = string.Empty;
        public int Severity { get; set; }
        public string? Description { get; set; }
        public string? SourceIP { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public ConfidenceTier ConfidenceTier { get; set; } = ConfidenceTier.Unverified;
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public string? RejectionReason { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public User Reporter { get; set; } = null!;
        public Location Location { get; set; } = null!;
        public AttackEvent? AttackEvent { get; set; }
    }
}