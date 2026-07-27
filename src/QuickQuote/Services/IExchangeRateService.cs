using QuickQuote.Models;

namespace QuickQuote.Services;

/// <summary>
/// Converts a USD amount into a set of target currencies using a live, third-party
/// exchange-rate API. Isolated behind an interface so the API provider (or a mock)
/// can be swapped without touching controllers or views.
/// </summary>
public interface IExchangeRateService
{
    Task<ExchangeRateResult> ConvertAsync(decimal amountUsd, IReadOnlyList<string> targetCurrencies, CancellationToken cancellationToken = default);
}
