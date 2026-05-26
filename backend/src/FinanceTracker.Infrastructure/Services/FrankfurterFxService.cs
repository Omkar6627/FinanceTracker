using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceTracker.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// FX rates from the free Frankfurter API (https://www.frankfurter.app). Rates are cached
/// in-process for a few hours; on any failure we fall back to an identity rate so the UI
/// keeps working (amounts simply display in the base currency).
/// </summary>
public class FrankfurterFxService : IFxRateService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    private static readonly ConcurrentDictionary<string, (DateTimeOffset fetchedAt, FxRates rates)> _cache = new();

    private readonly HttpClient _http;
    private readonly ILogger<FrankfurterFxService> _logger;

    public FrankfurterFxService(HttpClient http, ILogger<FrankfurterFxService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<FxRates> GetRatesAsync(string baseCurrency, CancellationToken ct = default)
    {
        var code = (baseCurrency ?? "USD").Trim().ToUpperInvariant();
        if (code.Length != 3) code = "USD";

        if (_cache.TryGetValue(code, out var entry) && DateTimeOffset.UtcNow - entry.fetchedAt < CacheTtl)
            return entry.rates;

        try
        {
            var dto = await _http.GetFromJsonAsync<FrankfurterResponse>($"latest?from={code}", ct);
            if (dto?.Rates is { Count: > 0 })
            {
                var rates = new FxRates(code, dto.Date, dto.Rates);
                _cache[code] = (DateTimeOffset.UtcNow, rates);
                return rates;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "FX rate fetch failed for {Base}; using identity fallback", code);
        }

        return new FxRates(code, DateTime.UtcNow.Date, new Dictionary<string, decimal> { [code] = 1m });
    }

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("base")] public string Base { get; set; } = "USD";
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("rates")] public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
