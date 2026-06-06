namespace ThreatCastPK.API.Services
{
    public class AbuseIPDBService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AbuseIPDBService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["AbuseIPDB:ApiKey"] ?? "";
        }

        public async Task<int> GetAbuseConfidenceScore(string ipAddress)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(ipAddress))
                return 0;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.abuseipdb.com/api/v2/check?ipAddress={ipAddress}&maxAgeInDays=90");

                request.Headers.Add("Key", _apiKey);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return 0;

                var json = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<AbuseIPDBResponse>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.Data?.AbuseConfidenceScore ?? 0;
            }
            catch
            {
                // Graceful degradation — if API is down return 0
                return 0;
            }
        }
    }

    public class AbuseIPDBResponse
    {
        public AbuseIPDBData? Data { get; set; }
    }

    public class AbuseIPDBData
    {
        public int AbuseConfidenceScore { get; set; }
        public string? IpAddress { get; set; }
        public bool IsPublic { get; set; }
        public int TotalReports { get; set; }
    }
}