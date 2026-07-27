using QuickQuote.Models;

namespace QuickQuote.Services;

/// <summary>
/// Wraps the payment provider integration (Razorpay, test mode) behind an
/// interface so controllers never touch the provider SDK directly, and the
/// provider could be swapped for another gateway without changing any calling
/// code. That swap already happened once in this project — see the README.
/// </summary>
public interface IPaymentService
{
    Task<PaymentSessionResult> CreateCheckoutSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a payment from the raw query-string parameters the provider
    /// appended when redirecting the buyer back (provider-specific — e.g.
    /// Razorpay's signed payment-link callback params). The confirmation logic,
    /// including signature verification, is entirely owned by the implementation.
    /// </summary>
    Task<PaymentConfirmation> ConfirmAsync(IReadOnlyDictionary<string, string> callbackParameters, CancellationToken cancellationToken = default);
}
