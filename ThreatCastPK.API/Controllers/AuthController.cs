using Microsoft.AspNetCore.Mvc;
﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using System.Text;
using ThreatCastPK.API.DTOs;
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

        public AuthController(ThreatCastDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // CRUD 4 — User Registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { message = "Email already registered." });

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return Conflict(new { message = "Username already taken." });

            // Validate password length
            if (dto.Password.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters." });

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Registered,
                ReputationScore = 0,
                IsSuspended = false,
                JoinDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { message = "Registration successful. Please log in." });
        }

        // CRUD 4 — User Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            if (user.IsSuspended)
                return Unauthorized(new { message = "Your account has been suspended. Contact admin." });

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString()
            });
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


        // GET /api/auth/google-login
        // Redirects the browser to Google's consent screen
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

    }
}