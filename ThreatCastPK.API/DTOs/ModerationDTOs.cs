namespace ThreatCastPK.API.DTOs
{
    public class RejectReportDTO
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ModerationReportResponseDTO
    {
        public Guid Id { get; set; }
        public string ReporterUsername { get; set; } = string.Empty;
        public string AttackType { get; set; } = string.Empty;
        public string TargetSector { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Severity { get; set; }
        public string? Description { get; set; }
        public string? SourceIP { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ConfidenceTier { get; set; } = string.Empty;
    }
<<<<<<< HEAD
=======

>>>>>>> haadi-cyber
}