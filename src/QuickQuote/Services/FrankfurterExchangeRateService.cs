using System.Text.Json.Serialization;
using QuickQuote.Models;

namespace QuickQuote.Services;

/// <summary>
/// Live currency conversion via the free, keyless Frankfurter API (https://frankfurter.app),
/// which republishes European Central Bank reference rates. No API key or auth is required,
/// which is why it was chosen for this demo over a keyed provider.
/// </summary>
public class FrankfurterExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterExchangeRateService> _logger;

    public FrankfurterExchangeRateService(HttpClient httpClient, ILogger<FrankfurterExchangeRateService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExchangeRateResult> ConvertAsync(decimal amountUsd, IReadOnlyList<string> targetCurrencies, CancellationToken cancellationToken = default)
    {
        if (targetCurrencies.Count == 0)
        {
            return new ExchangeRateResult { Success = true, Quotes = Array.Empty<ExchangeRateQuote>() };
        }

        var symbols = string.Join(",", targetCurrencies);

        try
        {
            using var response = await _httpClient.GetAsync($"latest?base=USD&symbols={symbols}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<FrankfurterResponse>(cancellationToken: cancellationToken);

            if (payload?.Rates is null || payload.Rates.Count == 0)
            {
                _logger.LogWarning("Frankfurter API returned no rates for symbols {Symbols}", symbols);
                return new ExchangeRateResult
                {
                    Success = false,
                    ErrorMessage = "The exchange-rate provider returned no data. Showing USD pricing only."
                };
            }

            var quotes = payload.Rates
                .Select(kv => new ExchangeRateQuote(kv.Key, kv.Value, Math.Round(amountUsd * kv.Value, 2)))
                .OrderBy(q => q.Currency)
                .ToList();

            return new ExchangeRateResult { Success = true, Quotes = quotes };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Exchange rate lookup failed");
            return new ExchangeRateResult
            {
                Success = false,
                ErrorMessage = "Live currency conversion is temporarily unavailable. Showing USD pricing only."
            };
        }
    }

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("base")]
        public string? Base { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
