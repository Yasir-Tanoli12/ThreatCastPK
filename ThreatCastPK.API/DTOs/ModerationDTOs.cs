public class ModerationReportResponseDTO
{
    public Guid Id { get; set; }
    public string ReporterUsername { get; set; } = string.Empty;
    public int ReporterReputation { get; set; }
    public string AttackType { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string? Description { get; set; }
    public string? SourceIP { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ConfidenceTier { get; set; } = string.Empty;

    // Added ML Integration properties
    public bool IsMlAnomaly { get; set; } = false;
    public double MlAnomalyScore { get; set; } = 0.0;
}
public class RejectReportDTO
{
    public string Reason { get; set; } = string.Empty;
}