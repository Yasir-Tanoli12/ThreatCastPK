using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.Database.Models
{
    public class SectorRiskScore
    {
        public Guid Id { get; set; }
        public string SectorName { get; set; } = string.Empty;
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
        public int EventCount { get; set; } = 0;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}