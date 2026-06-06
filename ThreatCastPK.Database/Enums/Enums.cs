namespace ThreatCastPK.Database.Enums
{
    public enum UserRole
    {
        Public,
        Registered,
        Reporter,
        Admin
    }

    public enum AttackType
    {
        Ransomware,
        Phishing,
        DDoS,
        IdentityTheft,
        Malware,
        Other
    }

    public enum Sector
    {
        Banking,
        Telecom,
        Healthcare,
        Education,
        Government,
        Energy,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum ConfidenceTier
    {
        Verified,
        CommunityReported,
        Unverified
    }

    public enum EventSource
    {
        API,
        Community,
        Admin
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AlertLevel
    {
        Medium,
        High,
        Critical
    }
}
