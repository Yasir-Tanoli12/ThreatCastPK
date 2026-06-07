// ThreatCastPK.Web/Services/ApiService.cs
// Central HTTP client wrapper — automatically attaches JWT to every request.
// All pages inject this instead of raw HttpClient.

using System.Net.Http.Json;
using System.Text.Json;

namespace ThreatCastPK.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    // Shared JSON options — matches how the API serializes enums as strings
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    // ----------------------------------------------------------------
    // Private helpers — attach Bearer token to every outgoing request
    // ----------------------------------------------------------------

    private async Task<HttpRequestMessage> BuildRequest(
        HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        var token = await _auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        if (body != null)
            request.Content = JsonContent.Create(body);

        return request;
    }

    // ----------------------------------------------------------------
    // Generic request methods — used by all the typed methods below
    // ----------------------------------------------------------------

    // Returns (data, null) on success, (default, errorMessage) on failure
    public async Task<(T? Data, string? Error)> GetAsync<T>(string url)
    {
        try
        {
            var request = await BuildRequest(HttpMethod.Get, url);
            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return (data, null);
            }

            var error = await ReadErrorAsync(response);
            return (default, error);
        }
        catch (Exception ex)
        {
            return (default, $"Request failed: {ex.Message}");
        }
    }

    public async Task<(T? Data, string? Error)> PostAsync<T>(string url, object body)
    {
        try
        {
            var request = await BuildRequest(HttpMethod.Post, url, body);
            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Some POST endpoints return 200/201 with no body
                if (response.Content.Headers.ContentLength == 0)
                    return (default, null);

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return (data, null);
            }

            var error = await ReadErrorAsync(response);
            return (default, error);
        }
        catch (Exception ex)
        {
            return (default, $"Request failed: {ex.Message}");
        }
    }

    // For POST endpoints that return no body (just a status code)
    public async Task<string?> PostAsync(string url, object body)
    {
        try
        {
            var request = await BuildRequest(HttpMethod.Post, url, body);
            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return null; // null = success

            return await ReadErrorAsync(response);
        }
        catch (Exception ex)
        {
            return $"Request failed: {ex.Message}";
        }
    }

    public async Task<string?> PutAsync(string url, object? body = null)
    {
        try
        {
            var request = await BuildRequest(HttpMethod.Put, url, body);
            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return null;

            return await ReadErrorAsync(response);
        }
        catch (Exception ex)
        {
            return $"Request failed: {ex.Message}";
        }
    }

    public async Task<(T? Data, string? Error)> PutAsync<T>(string url, object? body = null)
    {
        try
        {
            var request = await BuildRequest(HttpMethod.Put, url, body);
            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return (data, null);
            }

            var error = await ReadErrorAsync(response);
            return (default, error);
        }
        catch (Exception ex)
        {
            return (default, $"Request failed: {ex.Message}");
        }
    }

    public async Task<string?> PatchAsync(string url, object? body = null)
    {
        try
        {
            var request = await BuildRequest(HttpMethod.Patch, url, body);
            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return null;

            return await ReadErrorAsync(response);
        }
        catch (Exception ex)
        {
            return $"Request failed: {ex.Message}";
        }
    }

    public async Task<string?> DeleteAsync(string url)
    {
        try
        {
            var request = await BuildRequest(HttpMethod.Delete, url);
            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return null;

            return await ReadErrorAsync(response);
        }
        catch (Exception ex)
        {
            return $"Request failed: {ex.Message}";
        }
    }


    // ----------------------------------------------------------------
    // Typed API methods — pages call these directly
    // ----------------------------------------------------------------

    // Auth
    public Task<string?> RegisterAsync(string username, string email, string password)
        => PostAsync("/api/auth/register", new { username, email, password });

    // Profile
    public Task<(ProfileResponse? Data, string? Error)> GetProfileAsync()
        => GetAsync<ProfileResponse>("/api/profile");

    public Task<string?> UpdateProfileAsync(string username, string email)
        => PutAsync("/api/profile", new { username, email });

    public Task<string?> RequestReporterAsync()
        => PostAsync("/api/profile/request-reporter", new { });

    // Reports
    public Task<string?> SubmitReportAsync(object reportDto)
        => PostAsync("/api/reports", reportDto);

    public Task<(List<ReportResponse>? Data, string? Error)> GetMyReportsAsync(string? status = null)
    {
        var url = string.IsNullOrEmpty(status)
            ? "/api/reports/my"
            : $"/api/reports/my?status={status}";
        return GetAsync<List<ReportResponse>>(url);
    }

    // Analytics
    public Task<(StatsResponse? Data, string? Error)> GetStatsAsync()
        => GetAsync<StatsResponse>("/api/analytics/stats");

    public Task<(List<CityCountResponse>? Data, string? Error)> GetByCityAsync()
        => GetAsync<List<CityCountResponse>>("/api/analytics/by-city");

    public Task<(List<TypeCountResponse>? Data, string? Error)> GetByTypeAsync()
        => GetAsync<List<TypeCountResponse>>("/api/analytics/by-type");

    public Task<(List<TrendPointResponse>? Data, string? Error)> GetTrendAsync()
        => GetAsync<List<TrendPointResponse>>("/api/analytics/trend");

    public Task<(List<SectorRiskResponse>? Data, string? Error)> GetSectorRiskAsync()
        => GetAsync<List<SectorRiskResponse>>("/api/analytics/sector-risk");

    public Task<(List<RecentEventResponse>? Data, string? Error)> GetRecentEventsAsync()
    => GetAsync<List<RecentEventResponse>>("/api/analytics/recent-events");

    public Task<(List<MapEventResponse>? Data, string? Error)> GetEventsAsync(string timeFilter = "24h")
        => GetAsync<List<MapEventResponse>>($"/api/analytics/events?timeFilter={timeFilter}");

    // Subscriptions
    public Task<(List<SubscriptionResponse>? Data, string? Error)> GetSubscriptionsAsync()
        => GetAsync<List<SubscriptionResponse>>("/api/subscriptions");

    public Task<string?> CreateSubscriptionAsync(object dto)
        => PostAsync("/api/subscriptions", dto);

    public Task<string?> UpdateSubscriptionAsync(Guid id, object dto)
        => PutAsync($"/api/subscriptions/{id}", dto);

    public Task<string?> DeleteSubscriptionAsync(Guid id)
        => DeleteAsync($"/api/subscriptions/{id}");

    public Task<string?> ToggleSubscriptionAsync(Guid id)
        => PatchAsync($"/api/subscriptions/{id}/toggle");

    // Moderation
    public Task<(List<ModerationReportResponse>? Data, string? Error)> GetModerationQueueAsync()
        => GetAsync<List<ModerationReportResponse>>("/api/moderation/queue");

    public Task<string?> ApproveReportAsync(Guid id)
        => PutAsync($"/api/moderation/{id}/approve");

    public Task<string?> RejectReportAsync(Guid id, string reason)
        => PutAsync($"/api/moderation/{id}/reject", new { reason });

    public Task<(List<UserAdminResponse>? Data, string? Error)> GetAllUsersAsync()
        => GetAsync<List<UserAdminResponse>>("/api/moderation/users");

    public Task<string?> GrantReporterAsync(Guid id)
        => PutAsync($"/api/moderation/users/{id}/grant-reporter");

    public Task<string?> RevokeReporterAsync(Guid id)
        => PutAsync($"/api/moderation/users/{id}/revoke-reporter");

    public Task<string?> SuspendUserAsync(Guid id)
        => PutAsync($"/api/moderation/users/{id}/suspend");

    public Task<string?> UnsuspendUserAsync(Guid id)
        => PutAsync($"/api/moderation/users/{id}/unsuspend");

    public Task<(List<NotificationResponseDTO>? Data, string? Error)> GetNotificationsAsync()
        => GetAsync<List<NotificationResponseDTO>>("/api/notifications");

    // ----------------------------------------------------------------
    // Private helper — reads error message from response body
    // ----------------------------------------------------------------
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (body.TryGetProperty("message", out var msg))
                return msg.GetString() ?? response.ReasonPhrase ?? "Unknown error";
        }
        catch { }

        return (int)response.StatusCode switch
        {
            401 => "You are not logged in.",
            403 => "You do not have permission to do this.",
            404 => "Resource not found.",
            429 => "Too many requests. Please slow down.",
            500 => "Server error. Please try again later.",
            _ => response.ReasonPhrase ?? "Unknown error"
        };
    }
    // Forum
    public Task<(List<PostResponseDTO>? Data, string? Error)> GetForumPostsAsync(string? category = null)
    {
        var url = string.IsNullOrEmpty(category) || category == "All"
            ? "/api/forum/posts"
            : $"/api/forum/posts?category={Uri.EscapeDataString(category)}";
        return GetAsync<List<PostResponseDTO>>(url);
    }

    public Task<(PostResponseDTO? Data, string? Error)> GetForumPostAsync(Guid id)
        => GetAsync<PostResponseDTO>($"/api/forum/posts/{id}");

    public Task<string?> CreateForumPostAsync(string title, string content, string category)
        => PostAsync("/api/forum/posts", new { title, content, category });

    public Task<string?> CreateReplyAsync(Guid postId, string content)
        => PostAsync($"/api/forum/posts/{postId}/replies", new { content });

    public Task<string?> DeleteForumPostAsync(Guid id)
        => DeleteAsync($"/api/forum/posts/{id}");
}

// ----------------------------------------------------------------
// Response DTOs — mirror what the API returns
// These live here for simplicity; move to a Models/ folder if preferred
// ----------------------------------------------------------------

public class ProfileResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int ReputationScore { get; set; }
    public bool ReporterRequestPending { get; set; }
    public DateTime JoinDate { get; set; }
}

public class ReportResponse
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AttackTypes { get; set; } = string.Empty;
    public string Cities { get; set; } = string.Empty;
    public string Sectors { get; set; } = string.Empty;
    public int MinimumSeverity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ModerationReportResponse
{
    public Guid Id { get; set; }
    public string ReporterUsername { get; set; } = string.Empty;
    public int ReporterReputation { get; set; }
    public string City { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string? SourceIP { get; set; }
    public string? Description { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class UserAdminResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int ReputationScore { get; set; }
    public bool ReporterRequestPending { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime JoinDate { get; set; }
}
public class StatsResponse
{
    public int TotalToday { get; set; }
    public string TopCity { get; set; } = string.Empty;
    public string TopAttackType { get; set; } = string.Empty;
    public string TopSector { get; set; } = string.Empty;
    public int TotalAllTime { get; set; }
}

public class CityCountResponse
{
    public string City { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TypeCountResponse
{
    public string AttackType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TrendPointResponse
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SectorRiskResponse
{
    public string Sector { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int EventCount { get; set; }
}

public class RecentEventResponse
{
    public string Time { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string Source { get; set; } = string.Empty;
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
}

public class ReplyResponseDTO
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
public class MapEventResponse
{
    public Guid Id { get; set; }
    public string AttackType { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public int Severity { get; set; }
    public DateTime OccurredAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Source { get; set; } = string.Empty;
}
public class NotificationResponseDTO
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string NotificationType { get; set; } = string.Empty;
}

