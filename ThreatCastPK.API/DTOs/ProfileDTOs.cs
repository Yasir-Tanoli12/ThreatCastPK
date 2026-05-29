namespace ThreatCastPK.API.DTOs
{
    public class UpdateProfileDTO
    {
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class ProfileResponseDTO
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int ReputationScore { get; set; }
        public bool ReporterRequestPending { get; set; }
        public DateTime JoinDate { get; set; }
    }

    public class RequestReporterDTO
    {
        public string Reason { get; set; } = string.Empty;
    }
}