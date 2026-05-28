using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.Database.Models
{
    public class AttackEvent
    {
        public Guid Id { get; set; }
        public Guid LocationId { get; set; }
        public Guid? ReportId { get; set; }
        public Guid? CampaignId { get; set; }
        public AttackType AttackType { get; set; }
        public Sector TargetSector { get; set; }
        public int Severity { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public ConfidenceTier ConfidenceTier { get; set; }
        public EventSource Source { get; set; }

        // Navigation properties
        public Location Location { get; set; } = null!;
        public AttackReport? AttackReport { get; set; }
        public ThreatCampaign? ThreatCampaign { get; set; }
    }
}