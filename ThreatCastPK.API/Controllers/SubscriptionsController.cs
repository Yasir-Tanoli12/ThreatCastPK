using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ThreatCastDbContext _context;

        public SubscriptionsController(ThreatCastDbContext context)
        {
            _context = context;
        }

        // CRUD 6 — Create Subscription
        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDTO dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (dto.MinimumSeverity < 1 || dto.MinimumSeverity > 5)
                return BadRequest(new { message = "Minimum severity must be between 1 and 5." });

            if (string.IsNullOrEmpty(dto.AttackTypes))
                return BadRequest(new { message = "At least one attack type is required." });

            // Check max 3 subscriptions per user
            var count = await _context.AlertSubscriptions
                .CountAsync(s => s.UserId == userId);

            if (count >= 3)
                return UnprocessableEntity(new { message = "Maximum of 3 subscriptions allowed per user." });

            var subscription = new AlertSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AttackTypes = dto.AttackTypes,
                Cities = dto.Cities,
                Sectors = dto.Sectors,
                MinimumSeverity = dto.MinimumSeverity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AlertSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMySubscriptions), new { message = "Subscription created." },
                new SubscriptionResponseDTO
                {
                    Id = subscription.Id,
                    AttackTypes = subscription.AttackTypes,
                    Cities = subscription.Cities,
                    Sectors = subscription.Sectors,
                    MinimumSeverity = subscription.MinimumSeverity,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt
                });
        }

        // CRUD 6 — View My Subscriptions
        [HttpGet]
        public async Task<IActionResult> GetMySubscriptions()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var subscriptions = await _context.AlertSubscriptions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SubscriptionResponseDTO
                {
                    Id = s.Id,
                    AttackTypes = s.AttackTypes,
                    Cities = s.Cities,
                    Sectors = s.Sectors,
                    MinimumSeverity = s.MinimumSeverity,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(subscriptions);
        }

        // CRUD 7 — Update Subscription
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionDTO dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var subscription = await _context.AlertSubscriptions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return NotFound(new { message = "Subscription not found." });

            // Verify ownership
            if (subscription.UserId != userId)
                return Forbid();

            if (dto.MinimumSeverity < 1 || dto.MinimumSeverity > 5)
                return BadRequest(new { message = "Minimum severity must be between 1 and 5." });

            if (string.IsNullOrEmpty(dto.AttackTypes))
                return BadRequest(new { message = "At least one attack type is required." });

            subscription.AttackTypes = dto.AttackTypes;
            subscription.Cities = dto.Cities;
            subscription.Sectors = dto.Sectors;
            subscription.MinimumSeverity = dto.MinimumSeverity;
            subscription.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Subscription updated successfully." });
        }

        // CRUD 7 — Delete Subscription
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubscription(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var subscription = await _context.AlertSubscriptions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return NotFound(new { message = "Subscription not found." });

            // Verify ownership
            if (subscription.UserId != userId)
                return Forbid();

            _context.AlertSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subscription deleted successfully." });
        }

        // CRUD 7 — Toggle Subscription Active State
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleSubscription(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var subscription = await _context.AlertSubscriptions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return NotFound(new { message = "Subscription not found." });

            if (subscription.UserId != userId)
                return Forbid();

            subscription.IsActive = !subscription.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Subscription {(subscription.IsActive ? "activated" : "deactivated")}.", isActive = subscription.IsActive });
        }
    }
}