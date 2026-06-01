namespace ThreatCastPK.API.DTOs
{
    public class SubmitReportDTO
    {
        public string AttackType { get; set; } = string.Empty;
        public string TargetSector { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Severity { get; set; }
        public string? Description { get; set; }
        public string? SourceIP { get; set; }
    }

    public class ReportResponseDTO
    {
        public Guid Id { get; set; }
        public string AttackType { get; set; } = string.Empty;
        public string TargetSector { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Severity { get; set; }
        public string? Description { get; set; }
        public string? SourceIP { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ConfidenceTier { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
    }
}