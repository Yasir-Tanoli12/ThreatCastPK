<<<<<<< HEAD
﻿using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.Database.Models
{
    public class ThreatAdvisory
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string SeverityTag { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public string AffectedSectors { get; set; } = string.Empty;
        public bool IsArchived { get; set; } = false;

        // Navigation properties
        public User Admin { get; set; } = null!;
    }
=======
﻿using ThreatCastPK.Database.Models;

public class ThreatAdvisory
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SeverityTag { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public string AffectedSectors { get; set; } = string.Empty;
    public string AffectedCities { get; set; } = string.Empty;  // new
    public bool IsArchived { get; set; } = false;
    public User Admin { get; set; } = null!;
>>>>>>> haadi-cyber
}