using System.ComponentModel.DataAnnotations;

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
        public bool IsGoogleLinked { get; set; }
    }

    public class RequestReporterDTO
    {
        public string Reason { get; set; } = string.Empty;
    }
    public class ChangePasswordDTO
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}