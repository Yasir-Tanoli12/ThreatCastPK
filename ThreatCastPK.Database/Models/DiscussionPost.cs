namespace ThreatCastPK.Database.Models
{
    public class DiscussionPost
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public User User { get; set; } = null!;
    }
}