// ThreatCastPK.API/DTOs/AnalyticsDTOs.cs
namespace ThreatCastPK.API.DTOs;

public class StatsResponseDTO
{
    public int TotalToday { get; set; }
    public string TopCity { get; set; } = string.Empty;
    public string TopAttackType { get; set; } = string.Empty;
    public string TopSector { get; set; } = string.Empty;
    public int TotalAllTime { get; set; }
}

public class CityCountDTO
{
    public string City { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TypeCountDTO
{
    public string AttackType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TrendPointDTO
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SectorRiskDTO
{
    public string Sector { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int EventCount { get; set; }
}

public class RecentEventDTO
{
    public string Time { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string Source { get; set; } = string.Empty;
}