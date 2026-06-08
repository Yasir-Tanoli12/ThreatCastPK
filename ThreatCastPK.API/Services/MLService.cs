// File Path: ThreatCastPK.API/Services/MLService.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreatCastPK.API.Services;

public class AttackEventInput
{
    [JsonPropertyName("attack_type")]
    public string AttackType { get; set; } = string.Empty;

    [JsonPropertyName("anomaly_score")]
    public double AnomalyScore { get; set; }

    [JsonPropertyName("packet_length")]
    public double PacketLength { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("geo_location")]
    public string GeoLocation { get; set; } = string.Empty;

    [JsonPropertyName("network_segment")]
    public string NetworkSegment { get; set; } = string.Empty;
}

public class CampaignResponse
{
    [JsonPropertyName("is_campaign")]
    public bool IsCampaign { get; set; }

    [JsonPropertyName("alert_level")]
    public string AlertLevel { get; set; } = string.Empty;

    [JsonPropertyName("anomaly_count")]
    public int AnomalyCount { get; set; }

    [JsonPropertyName("total_events")]
    public int TotalEvents { get; set; }

    [JsonPropertyName("anomaly_flags")]
    public List<bool> AnomalyFlags { get; set; } = new();

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class MLService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<MLService> _logger;

    public MLService(HttpClient http, IConfiguration config, ILogger<MLService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;

        var baseUrl = _config["MlService:BaseUrl"] ?? "http://localhost:8000";
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<CampaignResponse?> DetectCampaignAsync(List<AttackEventInput> events)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/detect-campaign", new { events });
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[MLService] Request failed with status code {Code}", response.StatusCode);
                return GetFallbackResponse(events.Count);
            }

            return await response.Content.ReadFromJsonAsync<CampaignResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MLService] Error connecting to ML FastAPI service.");
            return GetFallbackResponse(events.Count);
        }
    }

    private static CampaignResponse GetFallbackResponse(int count)
    {
        // Graceful degradation (NFR-15): Return non-anomalous flags if ML service is down
        return new CampaignResponse
        {
            IsCampaign = false,
            AlertLevel = "NORMAL",
            AnomalyCount = 0,
            TotalEvents = count,
            AnomalyFlags = Enumerable.Repeat(false, count).ToList(),
            Message = "ML service offline - returned fallback."
        };
    }
}