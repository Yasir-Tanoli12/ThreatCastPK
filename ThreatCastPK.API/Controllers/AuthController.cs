using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using System.Text;
using ThreatCastPK.API.DTOs;
using ThreatCastPK.API.Services;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Models;
using ThreatCastPK.Database.Enums;

namespace ThreatCastPK.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ThreatCastDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthController(
            ThreatCastDbContext context,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        // CRUD 4 — User Registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
                return Conflict(new { message = "An account with this email already exists." });

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username.Trim()))
                return Conflict(new { message = "This username is already taken." });

            var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Username = dto.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Registered,
                ReputationScore = 0,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
                JoinDate = DateTime.UtcNow       // ← was CreatedAt, fixed to JoinDate
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var apiBase = _configuration["App:BaseUrl"] ?? "https://localhost:7001";
            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={verificationToken}&email={Uri.EscapeDataString(normalizedEmail)}";

            var emailHtml = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <div style="background: #0f172a; padding: 24px; border-radius: 8px 8px 0 0;">
                        <h1 style="color: #22d3ee; margin: 0;">ThreatCast PK</h1>
                        <p style="color: #94a3b8; margin: 4px 0 0;">Live Cyberattack Intelligence for Pakistan</p>
                    </div>
                    <div style="background: #1e293b; padding: 32px; border-radius: 0 0 8px 8px;">
                        <h2 style="color: #f1f5f9;">Verify your email address</h2>
                        <p style="color: #94a3b8;">Welcome, {user.Username}. Click the button below to confirm your email and activate your account.</p>
                        <a href="{verifyUrl}"
                           style="display: inline-block; background: #22d3ee; color: #0f172a;
                                  padding: 12px 28px; border-radius: 6px; text-decoration: none;
                                  font-weight: bold; margin: 16px 0;">
                            Verify My Email
                        </a>
                        <p style="color: #64748b; font-size: 13px;">This link expires in 24 hours. If you didn't create this account, ignore this email.</p>
                        <p style="color: #64748b; font-size: 12px;">Or copy this link: {verifyUrl}</p>
                    </div>
                </div>
                """;

            await _emailService.SendAsync(normalizedEmail, "Verify your ThreatCast PK account", emailHtml);

            return StatusCode(201, new
            {
                message = "Account created. Please check your email to verify your account before logging in."
            });
        }

        // CRUD 4 — User Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            if (!user.IsEmailVerified)
                return Unauthorized(new
                {
                    message = "Please verify your email before logging in. Check your inbox for the verification link.",
                    code = "EMAIL_NOT_VERIFIED"
                });

            if (user.IsSuspended)
                return Unauthorized(new { message = "Your account has been suspended. Contact admin." });

            var token = GenerateJwtToken(user);

            // Fire-and-forget login notification — never blocks the login response
            _ = Task.Run(async () =>
            {
                var loginHtml = $"""
                    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                        <div style="background: #0f172a; padding: 24px; border-radius: 8px 8px 0 0;">
                            <h1 style="color: #22d3ee; margin: 0;">ThreatCast PK</h1>
                        </div>
                        <div style="background: #1e293b; padding: 32px; border-radius: 0 0 8px 8px;">
                            <h2 style="color: #f1f5f9;">New login to your account</h2>
                            <p style="color: #94a3b8;">Hi {user.Username}, a login was just recorded on your ThreatCast PK account.</p>
                            <table style="color: #94a3b8; border-collapse: collapse; width: 100%; margin: 16px 0;">
                                <tr>
                                    <td style="padding: 8px 0; color: #64748b; width: 80px;">Time</td>
                                    <td>{DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC</td>
                                </tr>
                            </table>
                            <p style="color: #ef4444; font-size: 13px;">If this wasn't you, please change your password immediately.</p>
                        </div>
                    </div>
                    """;

                await _emailService.SendAsync(user.Email, "ThreatCast PK — New login detected", loginHtml);
            });

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                UserId = user.Id.ToString()
            });
        }

        // GET /api/auth/verify-email
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, [FromQuery] string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail
                                       && u.EmailVerificationToken == token);

            if (user == null)
                return BadRequest(new { message = "Invalid verification link." });

            if (user.IsEmailVerified)
            {
                var blazorAlready = _configuration["App:BlazorBaseUrl"] ?? "https://localhost:5262";
                return Redirect($"{blazorAlready}/login?verified=true");
            }

            if (user.EmailVerificationExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "This verification link has expired. Please request a new one." });

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;
            await _context.SaveChangesAsync();

            var blazorUrl = _configuration["App:BlazorBaseUrl"] ?? "https://localhost:5262";
            return Redirect($"{blazorUrl}/login?verified=true");
        }

        // POST /api/auth/resend-verification
        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDTO dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            // Always return 200 — never reveal whether the email exists
            if (user == null || user.IsEmailVerified)
                return Ok(new { message = "If that email is registered and unverified, a new link has been sent." });

            var newToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            user.EmailVerificationToken = newToken;
            user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            var apiBase = _configuration["App:BaseUrl"] ?? "https://localhost:7001";
            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={newToken}&email={Uri.EscapeDataString(normalizedEmail)}";

            var emailHtml = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <div style="background: #0f172a; padding: 24px; border-radius: 8px 8px 0 0;">
                        <h1 style="color: #22d3ee; margin: 0;">ThreatCast PK</h1>
                        <p style="color: #94a3b8; margin: 4px 0 0;">Live Cyberattack Intelligence for Pakistan</p>
                    </div>
                    <div style="background: #1e293b; padding: 32px; border-radius: 0 0 8px 8px;">
                        <h2 style="color: #f1f5f9;">New verification link</h2>
                        <p style="color: #94a3b8;">Hi {user.Username}, here is your new email verification link.</p>
                        <a href="{verifyUrl}"
                           style="display: inline-block; background: #22d3ee; color: #0f172a;
                                  padding: 12px 28px; border-radius: 6px; text-decoration: none;
                                  font-weight: bold; margin: 16px 0;">
                            Verify My Email
                        </a>
                        <p style="color: #64748b; font-size: 13px;">This link expires in 24 hours.</p>
                        <p style="color: #64748b; font-size: 12px;">Or copy this link: {verifyUrl}</p>
                    </div>
                </div>
                """;

            await _emailService.SendAsync(normalizedEmail, "ThreatCast PK — New verification link", emailHtml);

            return Ok(new { message = "If that email is registered and unverified, a new link has been sent." });
        }

        // GET /api/auth/google-login
        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "Auth"),
                Items = { { "returnUrl", returnUrl ?? "/" } }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}