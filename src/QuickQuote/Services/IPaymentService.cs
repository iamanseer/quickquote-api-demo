using QuickQuote.Models;

namespace QuickQuote.Services;

/// <summary>
/// Wraps the payment provider integration (Stripe Checkout, test mode) behind an
/// interface so controllers never touch the Stripe SDK directly, and the provider
/// could be swapped for another gateway without changing any calling code.
/// </summary>
public interface IPaymentService
{
    Task<PaymentSessionResult> CreateCheckoutSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken = default);

    Task<PaymentConfirmation> GetConfirmationAsync(string sessionId, CancellationToken cancellationToken = default);
}
