using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.Database.Models
{
    public class ThreatCampaign
    {
        public Guid Id { get; set; }
        public string IpRange { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public string AffectedCities { get; set; } = string.Empty;
        public string AffectedSectors { get; set; } = string.Empty;
        public int ReportCount { get; set; } = 0;
        public AlertLevel AlertLevel { get; set; }

        // Navigation properties
        public ICollection<AttackEvent> AttackEvents { get; set; } = new List<AttackEvent>();
    }
}