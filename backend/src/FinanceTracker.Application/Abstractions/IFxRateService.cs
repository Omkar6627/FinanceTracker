namespace FinanceTracker.Application.Abstractions;

public record FxRates(string Base, DateTime Date, IReadOnlyDictionary<string, decimal> Rates);

/// <summary>Provides foreign-exchange rates relative to a base currency (display-only conversion).</summary>
public interface IFxRateService
{
    Task<FxRates> GetRatesAsync(string baseCurrency, CancellationToken ct = default);
}
