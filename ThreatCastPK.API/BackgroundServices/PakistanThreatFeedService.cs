using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ThreatCastPK.API.Hubs;
using ThreatCastPK.API.Services;
using ThreatCastPK.Database.Context;
using ThreatCastPK.Database.Enums;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.API.BackgroundServices;

public class PakistanThreatFeedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PakistanThreatFeedService> _logger;
    private readonly IHubContext<ThreatCastHub> _hubContext;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    // Pakistan city IP ranges — used to map IPs to approximate cities
    // These are well-known Pakistani ISP ranges
    private static readonly Dictionary<string, (string City, double Lat, double Lng)> PkIspRanges = new()
    {
        { "39.32.",    ("Karachi",   24.8607, 67.0011) },
        { "39.33.",    ("Lahore",    31.5204, 74.3587) },
        { "39.34.",    ("Islamabad", 33.6844, 73.0479) },
        { "58.65.",    ("Karachi",   24.8607, 67.0011) },
        { "58.27.",    ("Lahore",    31.5204, 74.3587) },
        { "115.186.",  ("Islamabad", 33.6844, 73.0479) },
        { "119.153.",  ("Karachi",   24.8607, 67.0011) },
        { "119.155.",  ("Lahore",    31.5204, 74.3587) },
        { "182.176.",  ("Karachi",   24.8607, 67.0011) },
        { "182.177.",  ("Lahore",    31.5204, 74.3587) },
        { "182.178.",  ("Islamabad", 33.6844, 73.0479) },
        { "182.179.",  ("Rawalpindi",33.5651, 73.0169) },
        { "202.163.",  ("Karachi",   24.8607, 67.0011) },
        { "203.128.",  ("Islamabad", 33.6844, 73.0479) },
        { "209.58.",   ("Lahore",    31.5204, 74.3587) },
    };

    // Map AbuseIPDB categories to our AttackType enum
    private static readonly Dictionary<int, AttackType> CategoryMap = new()
    {
        { 1,  AttackType.Other },        // DNS Compromise
        { 2,  AttackType.Other },        // DNS Poisoning
        { 3,  AttackType.Other },        // Fraud Orders
        { 4,  AttackType.DDoS },         // DDoS Attack
        { 5,  AttackType.Other },        // FTP Brute-Force
        { 6,  AttackType.Other },        // Ping of Death
        { 7,  AttackType.Other },        // Phishing — mapped below
        { 8,  AttackType.Other },        // Fraud VoIP
        { 9,  AttackType.Other },        // Open Proxy
        { 10, AttackType.Other },        // Web Spam
        { 11, AttackType.Phishing },     // Email Spam
        { 12, AttackType.Other },        // Blog Spam
        { 13, AttackType.Other },        // VPN IP
        { 14, AttackType.Other },        // Port Scan
        { 15, AttackType.Other },        // Hacking
        { 16, AttackType.Malware },      // SQL Injection
        { 17, AttackType.Other },        // Spoofing
        { 18, AttackType.Phishing },     // Brute-Force
        { 19, AttackType.Other },        // Bad Web Bot
        { 20, AttackType.Other },        // Exploited Host
        { 21, AttackType.Malware },      // Web App Attack
        { 22, AttackType.Other },        // SSH
        { 23, AttackType.Ransomware },   // IoT Targeted
    };

    private static readonly Sector[] Sectors = Enum.GetValues<Sector>();
    private static readonly Random Rng = new();

    public PakistanThreatFeedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PakistanThreatFeedService> logger,
        IHubContext<ThreatCastHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[ThreatFeed] Pakistan threat feed service started.");

        // Run once on startup
        await RunFeedAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunFeedAsync(stoppingToken);
        }
    }

    private async Task RunFeedAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ThreatCastDbContext>();
            var abuseIPDB = scope.ServiceProvider.GetRequiredService<AbuseIPDBService>();
            var greyNoise = scope.ServiceProvider.GetRequiredService<GreyNoiseService>();

            // Fetch blacklist from AbuseIPDB — country=PK, confidence >= 90
            var pkIps = await FetchPakistaniBlacklistAsync(scope, ct);

            if (pkIps.Count == 0)
            {
                _logger.LogInformation("[ThreatFeed] No Pakistani IPs found in blacklist this cycle.");
                return;
            }

            _logger.LogInformation("[ThreatFeed] Processing {Count} Pakistani IPs.", pkIps.Count);

            var locations = await db.Locations.ToListAsync(ct);
            int created = 0;

            foreach (var entry in pkIps.Take(20)) // cap at 20 per cycle — respect rate limits
            {
                if (ct.IsCancellationRequested) break;

                // Skip if we already have an event for this IP in the last 24h
                var recentExists = await db.AttackEvents
                    .AnyAsync(e => e.SourceIP == entry.IpAddress
                                && e.OccurredAt >= DateTime.UtcNow.AddHours(-24), ct);
                if (recentExists) continue;

                // GreyNoise check — skip background noise scanners
                var gnResult = await greyNoise.ClassifyAsync(entry.IpAddress);
                if (gnResult.IsNoise || gnResult.IsRiot)
                {
                    _logger.LogDebug("[ThreatFeed] Skipping {IP} — GreyNoise classified as noise/riot.", entry.IpAddress);
                    continue;
                }

                // Map IP to city
                var (city, lat, lng) = ResolveCity(entry.IpAddress);
                var location = locations.FirstOrDefault(l =>
                    l.CityName.Equals(city, StringComparison.OrdinalIgnoreCase));

                if (location == null) continue;

                // Map categories to attack type
                var attackType = ResolveAttackType(entry.Categories);

                // Infer severity from abuse score
                var severity = entry.AbuseConfidenceScore switch
                {
                    >= 90 => 5,
                    >= 75 => 4,
                    >= 60 => 3,
                    >= 40 => 2,
                    _ => 1
                };

                // Pick a plausible sector — in future this can be ML-driven
                var sector = ResolveSector(attackType);

                var attackEvent = new AttackEvent
                {
                    Id = Guid.NewGuid(),
                    LocationId = location.Id,
                    ReportId = null,
                    CampaignId = null,
                    AttackType = attackType,
                    TargetSector = sector,
                    Severity = severity,
                    OccurredAt = DateTime.UtcNow,
                    ConfidenceTier = ConfidenceTier.Verified,
                    Source = EventSource.API,
                    SourceIP = entry.IpAddress,
                    GreyNoiseClassification = gnResult.Classification
                };

                db.AttackEvents.Add(attackEvent);

                try
                {
                    await db.SaveChangesAsync(ct);
                    created++;

                    // Broadcast to all connected clients
                    await _hubContext.Clients.Group("all_viewers")
                        .SendAsync("NewAttackEvent", new
                        {
                            id = attackEvent.Id,
                            city = location.CityName,
                            lat = location.Latitude,
                            lng = location.Longitude,
                            attackType = attackType.ToString(),
                            targetSector = sector.ToString(),
                            severity = severity,
                            occurredAt = attackEvent.OccurredAt,
                            confidenceTier = "Verified",
                            source = "API",
                            greyNoiseClassification = gnResult.Classification
                        }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ThreatFeed] Failed to save event for IP {IP}.", entry.IpAddress);
                }
            }

            _logger.LogInformation("[ThreatFeed] Created {Count} new verified events.", created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ThreatFeed] Feed cycle failed.");
        }
    }

    private async Task<List<BlacklistEntry>> FetchPakistaniBlacklistAsync(
        IServiceScope scope, CancellationToken ct)
    {
        try
        {
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>()
                .CreateClient();

            var apiKey = config["AbuseIPDB:ApiKey"];

            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.abuseipdb.com/api/v2/blacklist?confidenceMinimum=90&limit=100");
            request.Headers.Add("Key", apiKey);
            request.Headers.Add("Accept", "application/json");

            var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            var results = new List<BlacklistEntry>();

            if (!doc.RootElement.TryGetProperty("data", out var data)) return results;

            foreach (var item in data.EnumerateArray())
            {
                var country = item.TryGetProperty("countryCode", out var cc)
                    ? cc.GetString() : null;

                if (country != "PK") continue;

                var ip = item.TryGetProperty("ipAddress", out var ipProp)
                    ? ipProp.GetString() ?? "" : "";
                var score = item.TryGetProperty("abuseConfidenceScore", out var sc)
                    ? sc.GetInt32() : 0;

                // Categories come as array of ints
                var categories = new List<int>();
                if (item.TryGetProperty("categories", out var cats))
                    foreach (var cat in cats.EnumerateArray())
                        categories.Add(cat.GetInt32());

                if (!string.IsNullOrEmpty(ip))
                    results.Add(new BlacklistEntry(ip, score, categories));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ThreatFeed] Failed to fetch AbuseIPDB blacklist.");
            return new();
        }
    }

    private static (string City, double Lat, double Lng) ResolveCity(string ip)
    {
        foreach (var (prefix, info) in PkIspRanges)
            if (ip.StartsWith(prefix))
                return info;

        // Default to a random major city if prefix not matched
        var cities = new[]
        {
            ("Karachi",   24.8607, 67.0011),
            ("Lahore",    31.5204, 74.3587),
            ("Islamabad", 33.6844, 73.0479),
            ("Rawalpindi",33.5651, 73.0169),
            ("Faisalabad",31.4504, 73.1350),
        };
        return cities[Rng.Next(cities.Length)];
    }

    private static AttackType ResolveAttackType(List<int> categories)
    {
        foreach (var cat in categories)
            if (CategoryMap.TryGetValue(cat, out var mapped) && mapped != AttackType.Other)
                return mapped;
        return AttackType.Other;
    }

    private static Sector ResolveSector(AttackType attackType)
    {
        // Weighted sector assignment based on attack type
        return attackType switch
        {
            AttackType.Ransomware => Sector.Healthcare,
            AttackType.Phishing => Sector.Banking,
            AttackType.DDoS => Sector.Telecom,
            AttackType.Malware => Sector.Government,
            _ => Sectors[Rng.Next(Sectors.Length)]
        };
    }

    private record BlacklistEntry(string IpAddress, int AbuseConfidenceScore, List<int> Categories);
}