using System.Text.Json;

namespace ThreatCastPK.API.Services;

public class GreyNoiseService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GreyNoiseService> _logger;

    public GreyNoiseService(HttpClient http, IConfiguration config,
        ILogger<GreyNoiseService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<GreyNoiseResult> ClassifyAsync(string ip)
    {
        try
        {
            var apiKey = _config["GreyNoise:ApiKey"];
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.greynoise.io/v3/community/{ip}");
            request.Headers.Add("key", apiKey);

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new GreyNoiseResult { Classification = "unknown" };

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            var noise = body.TryGetProperty("noise", out var n) && n.GetBoolean();
            var riot = body.TryGetProperty("riot", out var r) && r.GetBoolean();
            var classification = body.TryGetProperty("classification", out var c)
                ? c.GetString() ?? "unknown"
                : "unknown";

            return new GreyNoiseResult
            {
                IsNoise = noise,
                IsRiot = riot,
                Classification = classification
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[GreyNoise] Lookup failed for {IP}: {Msg}", ip, ex.Message);
            return new GreyNoiseResult { Classification = "unknown" };
        }
    }
}

public class GreyNoiseResult
{
    public bool IsNoise { get; set; }
    public bool IsRiot { get; set; }
    public string Classification { get; set; } = "unknown";
}