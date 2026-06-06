namespace ThreatCastPK.API.DTOs
{
    public class CreateSubscriptionDTO
    {
        public string AttackTypes { get; set; } = string.Empty;
        public string Cities { get; set; } = string.Empty;
        public string Sectors { get; set; } = string.Empty;
        public int MinimumSeverity { get; set; } = 1;
    }

    public class UpdateSubscriptionDTO
    {
        public string AttackTypes { get; set; } = string.Empty;
        public string Cities { get; set; } = string.Empty;
        public string Sectors { get; set; } = string.Empty;
        public int MinimumSeverity { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class SubscriptionResponseDTO
    {
        public Guid Id { get; set; }
        public string AttackTypes { get; set; } = string.Empty;
        public string Cities { get; set; } = string.Empty;
        public string Sectors { get; set; } = string.Empty;
        public int MinimumSeverity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}