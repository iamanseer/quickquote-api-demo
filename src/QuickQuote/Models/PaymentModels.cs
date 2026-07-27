namespace QuickQuote.Models;

public record PaymentSessionRequest(
    string ProductId,
    string ProductName,
    string ProductDescription,
    decimal AmountUsd,
    string SuccessUrl,
    string CancelUrl);

public class PaymentSessionResult
{
    public bool Success { get; init; }
    public string? CheckoutUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public class PaymentConfirmation
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CustomerEmail { get; init; }
    public string? PaymentStatus { get; init; }
    public long? AmountTotalCents { get; init; }
    public string? Currency { get; init; }
}
