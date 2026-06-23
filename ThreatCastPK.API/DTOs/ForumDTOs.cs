// ThreatCastPK.API/DTOs/ForumDTOs.cs
namespace ThreatCastPK.API.DTOs;

public class CreatePostDTO
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
}

public class CreateReplyDTO
{
    public string Content { get; set; } = string.Empty;
}

public class PostResponseDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ReplyCount { get; set; }
    public List<ReplyResponseDTO> Replies { get; set; } = new();
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public int ViewCount { get; set; }
    public bool IsPinned { get; set; }
    public bool IsFlagged { get; set; }
}

public class ReplyResponseDTO
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
}
public class FlagPostDTO
{
    public string Reason { get; set; } = string.Empty;
}