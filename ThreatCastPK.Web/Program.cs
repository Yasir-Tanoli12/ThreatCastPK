using Microsoft.AspNetCore.Components.Authorization;
using ThreatCastPK.Web.Components;
using ThreatCastPK.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();



// --- paste inside builder.Services block ---

// Set the base address so HttpClient calls /api/... resolve correctly
// Replace with your actual API URL in production
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
    ?? "https://localhost:7262")
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<AuthenticationStateProvider, ThreatCastAuthStateProvider>();
builder.Services.AddAuthorizationCore();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
// Proxy Google OAuth initiation to the API
// This keeps everything on the same origin (localhost:5000)
app.MapGet("/auth/google-initiate", (HttpContext ctx) =>
{
    ctx.Response.Redirect("http://localhost:5262/api/auth/google-login");
    return Task.CompletedTask;
});
app.Run();
