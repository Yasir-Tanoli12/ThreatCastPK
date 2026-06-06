using ThreatCastPK.Database.Enums;

<<<<<<< HEAD
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
=======
public class SectorRiskScore
{
    public Guid Id { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public int EventCount24h { get; set; } = 0;
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
>>>>>>> haadi-cyber
}