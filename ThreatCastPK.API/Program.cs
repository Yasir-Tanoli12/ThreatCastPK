<<<<<<< HEAD
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ThreatCastPK.Database.Context;
using ThreatCastPK.API.Hubs;
=======
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ThreatCastPK.API.BackgroundServices;
using ThreatCastPK.API.Hubs;
using ThreatCastPK.API.Services;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;
using ThreatCastPK.Database.Models;


>>>>>>> haadi-cyber

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ThreatCastDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
<<<<<<< HEAD
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
=======
builder.Services.AddAuthentication(options =>
{
    // Default scheme for API endpoints is still JWT
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
})
// Cookie handler is required for Google OAuth redirect flow
.AddCookie("ExternalCookies", options =>
{
    options.Cookie.Name = "tcpk.external";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/api/auth/google-callback";
    options.SignInScheme = "ExternalCookies";
    options.UsePkce = false;

    options.CorrelationCookie.Name = ".AspNetCore.Correlation.Google.";
    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CorrelationCookie.HttpOnly = true;
    options.CorrelationCookie.IsEssential = true;

    options.Events.OnTicketReceived = async context =>
    {
        

        var db = context.HttpContext.RequestServices
                        .GetRequiredService<ThreatCastDbContext>();
        var config = context.HttpContext.RequestServices
                        .GetRequiredService<IConfiguration>();

        var googleId = context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = context.Principal!.FindFirstValue(ClaimTypes.Email)!;
        var name = context.Principal!.FindFirstValue(ClaimTypes.Name)
                           ?? email.Split('@')[0];

        ;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId || u.Email == email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = name.Replace(" ", "_").ToLower() + "_" +
                                   Guid.NewGuid().ToString()[..4],
                PasswordHash = null,
                GoogleId = googleId,
                Role = UserRole.Registered,
                JoinDate = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            
        }
        else if (user.GoogleId == null)
        {
            user.GoogleId = googleId;
            await db.SaveChangesAsync();
            
        }
        else
        {
            
        }

        if (user.IsSuspended)
        {
            context.Response.Redirect("https://localhost:7130/login?error=suspended");
            context.HandleResponse();
            return;
        }

        var key = new SymmetricSecurityKey(
                           Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role.ToString())
        };
        var jwtToken = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        await context.HttpContext.SignOutAsync("ExternalCookies");

        var redirectUrl =
            $"https://localhost:7130/oauth-callback?token={token}" +
            $"&userId={user.Id}" +
            $"&username={Uri.EscapeDataString(user.Username)}" +
            $"&role={user.Role}";

        

        context.Response.Redirect(redirectUrl);
        context.HandleResponse(); // stops default processing
    };

    options.Events.OnRemoteFailure = context =>
    {
        Console.WriteLine($"=== REMOTE FAILURE: {context.Failure?.Message} ===");
        context.Response.Redirect("https://localhost:7130/login?error=google_failed");
        context.HandleResponse();
        return Task.CompletedTask;
    };
});
>>>>>>> haadi-cyber

// Authorization
builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers();

// SignalR
builder.Services.AddSignalR();
// AbuseIPDB Service
builder.Services.AddHttpClient<ThreatCastPK.API.Services.AbuseIPDBService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

<<<<<<< HEAD
// CORS — must allow credentials for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5000",
                           "http://localhost:5001",
                           "https://localhost:5001",
                           "http://localhost:5262")
=======
// Persist DataProtection keys so OAuth state survives app restarts
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
Directory.CreateDirectory(keysFolder);

builder.Services.AddDataProtection()
    .SetApplicationName("ThreatCastPK")
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysFolder));

// CORS — must allow credentials for SignalR
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? new[]
{
    "http://localhost:5000",
    "http://localhost:5001",
    "https://localhost:5001",
    "http://localhost:5262",
    "https://localhost:7130",
    "http://localhost:5136"
};

    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
>>>>>>> haadi-cyber
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

<<<<<<< HEAD
=======
// Background services
builder.Services.AddSingleton<NotificationChannel>();
builder.Services.AddHostedService<NotificationDispatchService>();
builder.Services.AddHostedService<SectorRiskScoringService>();
builder.Services.AddHttpClient<GreyNoiseService>();

>>>>>>> haadi-cyber
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

<<<<<<< HEAD
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
=======
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.Always,
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Google OAuth callback — must be a minimal API endpoint, NOT an MVC controller
// so the OAuth middleware processes the state/cookie BEFORE this code runs


>>>>>>> haadi-cyber
app.MapControllers();
app.MapHub<ThreatCastHub>("/hubs/threatcast");
app.Run();