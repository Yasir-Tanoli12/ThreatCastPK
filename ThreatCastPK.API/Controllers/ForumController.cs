using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.Controllers;

[ApiController]
[Route("api/forum")]
public class ForumController : ControllerBase
{
    private readonly ThreatCastDbContext _context;

    public ForumController(ThreatCastDbContext context)
    {
        _context = context;
    }

    // GET /api/forum/posts?category=&sort=new|hot|top
    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string? category = null,
        [FromQuery] string sort = "new")
    {
        var query = _context.DiscussionPosts
            .Include(p => p.User)
            .Where(p => !p.IsDeleted && p.ParentPostId == null);

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => p.Category == category);

        var posts = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                p.Category,
                p.CreatedAt,
                p.Upvotes,
                p.Downvotes,
                p.ViewCount,
                p.IsPinned,
                p.IsFlagged,
                p.User.Username,
                Role = p.User.Role.ToString(),
                ReplyCount = _context.DiscussionPosts
                    .Count(r => r.ParentPostId == p.Id && !r.IsDeleted)
            })
            .ToListAsync();

        // Sort in memory — EF can't translate the hot score formula
        var sorted = sort switch
        {
            "hot" => posts.OrderByDescending(p => p.IsPinned)
                         .ThenByDescending(p =>
                             (p.Upvotes - p.Downvotes) /
                             Math.Pow((DateTime.UtcNow - p.CreatedAt).TotalHours + 2, 1.5))
                         .ToList(),
            "top" => posts.OrderByDescending(p => p.IsPinned)
                         .ThenByDescending(p => p.Upvotes - p.Downvotes)
                         .ToList(),
            _ => posts.OrderByDescending(p => p.IsPinned)
                     .ThenByDescending(p => p.CreatedAt)
                     .ToList()
        };

        var result = sorted.Select(p => new PostResponseDTO
        {
            Id = p.Id,
            Title = p.Title,
            Content = p.Content,
            Category = p.Category,
            AuthorUsername = p.Username,
            AuthorRole = p.Role,
            CreatedAt = p.CreatedAt,
            ReplyCount = p.ReplyCount,
            Upvotes = p.Upvotes,
            Downvotes = p.Downvotes,
            ViewCount = p.ViewCount,
            IsPinned = p.IsPinned,
            IsFlagged = p.IsFlagged
        }).ToList();

        return Ok(result);
    }

    // GET /api/forum/posts/{id}
    [HttpGet("posts/{id:guid}")]
    public async Task<IActionResult> GetPost(Guid id)
    {
        var post = await _context.DiscussionPosts
            .Include(p => p.User)
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound(new { message = "Post not found." });

        // Increment view count
        post.ViewCount++;
        await _context.SaveChangesAsync();

        var replies = await _context.DiscussionPosts
            .Include(r => r.User)
            .Where(r => r.ParentPostId == id && !r.IsDeleted)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ReplyResponseDTO
            {
                Id = r.Id,
                Content = r.Content,
                AuthorUsername = r.User.Username,
                AuthorRole = r.User.Role.ToString(),
                CreatedAt = r.CreatedAt,
                Upvotes = r.Upvotes,
                Downvotes = r.Downvotes
            })
            .ToListAsync();

        return Ok(new PostResponseDTO
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Category = post.Category,
            AuthorUsername = post.User.Username,
            AuthorRole = post.User.Role.ToString(),
            CreatedAt = post.CreatedAt,
            ReplyCount = replies.Count,
            Replies = replies,
            Upvotes = post.Upvotes,
            Downvotes = post.Downvotes,
            ViewCount = post.ViewCount,
            IsPinned = post.IsPinned,
            IsFlagged = post.IsFlagged
        });
    }

    // POST /api/forum/posts
    [HttpPost("posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Title is required." });
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Content is required." });
        if (dto.Title.Length > 200)
            return BadRequest(new { message = "Title must be under 200 characters." });
        if (dto.Content.Length > 5000)
            return BadRequest(new { message = "Content must be under 5000 characters." });

        var validCategories = new[] { "General", "Threat Intel", "Malware Analysis",
                                       "Incident Reports", "Tools & Resources" };
        if (!validCategories.Contains(dto.Category))
            dto.Category = "General";

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.IsSuspended) return Forbid();

        var post = new DiscussionPost
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            Category = dto.Category,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.DiscussionPosts.Add(post);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPost), new { id = post.Id },
            new { message = "Post created successfully.", postId = post.Id });
    }

    // POST /api/forum/posts/{id}/replies
    [HttpPost("posts/{id:guid}/replies")]
    [Authorize]
    public async Task<IActionResult> CreateReply(Guid id, [FromBody] CreateReplyDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Reply content is required." });
        if (dto.Content.Length > 2000)
            return BadRequest(new { message = "Reply must be under 2000 characters." });

        var parentPost = await _context.DiscussionPosts
            .Where(p => p.Id == id && !p.IsDeleted && p.ParentPostId == null)
            .FirstOrDefaultAsync();

        if (parentPost == null)
            return NotFound(new { message = "Post not found." });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.IsSuspended) return Forbid();

        var reply = new DiscussionPost
        {
            Id = Guid.NewGuid(),
            Title = string.Empty,
            Content = dto.Content.Trim(),
            Category = parentPost.Category,
            UserId = userId,
            ParentPostId = id,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.DiscussionPosts.Add(reply);
        await _context.SaveChangesAsync();

        return Ok(new ReplyResponseDTO
        {
            Id = reply.Id,
            Content = reply.Content,
            AuthorUsername = user.Username,
            AuthorRole = user.Role.ToString(),
            CreatedAt = reply.CreatedAt
        });
    }

    // PUT /api/forum/posts/{id}/upvote
    [HttpPut("posts/{id:guid}/upvote")]
    [Authorize]
    public async Task<IActionResult> Upvote(Guid id)
    {
        var post = await _context.DiscussionPosts
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (post == null) return NotFound(new { message = "Post not found." });

        post.Upvotes++;
        await _context.SaveChangesAsync();
        return Ok(new { upvotes = post.Upvotes, downvotes = post.Downvotes });
    }

    // PUT /api/forum/posts/{id}/downvote
    [HttpPut("posts/{id:guid}/downvote")]
    [Authorize]
    public async Task<IActionResult> Downvote(Guid id)
    {
        var post = await _context.DiscussionPosts
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (post == null) return NotFound(new { message = "Post not found." });

        post.Downvotes++;
        await _context.SaveChangesAsync();
        return Ok(new { upvotes = post.Upvotes, downvotes = post.Downvotes });
    }

    // PUT /api/forum/posts/{id}/pin — Admin only
    [HttpPut("posts/{id:guid}/pin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TogglePin(Guid id)
    {
        var post = await _context.DiscussionPosts
            .Where(p => p.Id == id && !p.IsDeleted && p.ParentPostId == null)
            .FirstOrDefaultAsync();

        if (post == null) return NotFound(new { message = "Post not found." });

        post.IsPinned = !post.IsPinned;
        await _context.SaveChangesAsync();
        return Ok(new { isPinned = post.IsPinned });
    }

    // PUT /api/forum/posts/{id}/flag — Authenticated users
    [HttpPut("posts/{id:guid}/flag")]
    [Authorize]
    public async Task<IActionResult> FlagPost(Guid id, [FromBody] FlagPostDTO dto)
    {
        var post = await _context.DiscussionPosts
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (post == null) return NotFound(new { message = "Post not found." });

        post.IsFlagged = true;
        post.FlagReason = dto.Reason.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { message = "Post flagged for review." });
    }

    // DELETE /api/forum/posts/{id}
    [HttpDelete("posts/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var post = await _context.DiscussionPosts
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (post == null) return NotFound(new { message = "Post not found." });
        if (post.UserId != userId && role != "Admin") return Forbid();

        post.IsDeleted = true;

        if (post.ParentPostId == null)
        {
            var replies = await _context.DiscussionPosts
                .Where(r => r.ParentPostId == id)
                .ToListAsync();
            foreach (var reply in replies)
                reply.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Post deleted." });
    }
}