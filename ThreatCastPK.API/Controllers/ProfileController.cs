using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.Database.Context;

namespace ThreatCastPK.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ThreatCastDbContext _context;

        public ProfileController(ThreatCastDbContext context)
        {
            _context = context;
        }

        // CRUD 5 — Get Profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new ProfileResponseDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                ReputationScore = user.ReputationScore,
                ReporterRequestPending = user.ReporterRequestPending,
                JoinDate = user.JoinDate
            });
        }

        // CRUD 5 — Update Profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            // Check username not taken by someone else
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != userId))
                return Conflict(new { message = "Username already taken." });

            user.Username = dto.Username;

            // Only update email if provided and account is not Google-linked
            if (!string.IsNullOrEmpty(dto.Email) && user.GoogleId == null)
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
                    return Conflict(new { message = "Email already in use." });

                user.Email = dto.Email;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully." });
        }

        // CRUD 5 — Request Reporter Status
        [HttpPost("request-reporter")]
        public async Task<IActionResult> RequestReporterStatus()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.ReporterRequestPending)
                return BadRequest(new { message = "Reporter request already pending." });

            if (user.Role == ThreatCastPK.Database.Enums.UserRole.Reporter)
                return BadRequest(new { message = "You are already a reporter." });

            user.ReporterRequestPending = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reporter status request submitted. Admin will review your request." });
        }
    }
}