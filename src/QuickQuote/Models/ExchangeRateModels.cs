namespace QuickQuote.Models;

public record ExchangeRateQuote(string Currency, decimal Rate, decimal ConvertedAmount);

public class ExchangeRateResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ExchangeRateQuote> Quotes { get; init; } = Array.Empty<ExchangeRateQuote>();
}
