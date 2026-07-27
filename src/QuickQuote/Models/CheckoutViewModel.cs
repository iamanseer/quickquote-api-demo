namespace QuickQuote.Models;

public class CheckoutViewModel
{
    public required Product Product { get; init; }
    public required ExchangeRateResult ExchangeRates { get; init; }
    public string? PaymentError { get; init; }
}

public class ConfirmationViewModel
{
    public required Product Product { get; init; }
    public required PaymentConfirmation Payment { get; init; }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
