// ThreatCastPK.API/DTOs/ThreatAdvisoryDTOs.cs
namespace ThreatCastPK.API.DTOs;


public class CreateAdvisoryDTO
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;       // was Content
    public string SeverityTag { get; set; } = string.Empty; // was Severity
    public string? AffectedSectors { get; set; }
    public string? AffectedCities { get; set; }
}

public class AdvisoryResponseDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? AffectedSectors { get; set; }
    public string? AffectedCities { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsArchived { get; set; }
}