namespace ThreatCastPK.Database.Models
{
    public class DiscussionPost
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
<<<<<<< HEAD
        public string Content { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
=======

        // Required by ForumController and Forum.razor
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = "General";

        // Renamed from PostedAt to match controller queries
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

>>>>>>> haadi-cyber
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

<<<<<<< HEAD
        // Navigation properties
        public User User { get; set; } = null!;
=======
        // For threaded replies — null means it's a top-level post
        public Guid? ParentPostId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public DiscussionPost? ParentPost { get; set; }
>>>>>>> haadi-cyber
    }
}