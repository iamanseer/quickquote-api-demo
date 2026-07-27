using Microsoft.Extensions.Options;
using QuickQuote.Models;
using Stripe;
using Stripe.Checkout;

namespace QuickQuote.Services;

/// <summary>
/// Stripe Checkout integration, running against Stripe's test-mode API. The secret
/// key is read from configuration (user secrets locally, environment variables in
/// production) and is never hardcoded or committed.
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IOptions<StripeOptions> options, ILogger<StripePaymentService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentSessionResult> CreateCheckoutSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogWarning("Stripe secret key is not configured; cannot create a checkout session");
            return new PaymentSessionResult
            {
                Success = false,
                ErrorMessage = "Payments aren't configured yet — a Stripe test secret key is missing on the server."
            };
        }

        try
        {
            var sessionOptions = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)Math.Round(request.AmountUsd * 100m, MidpointRounding.AwayFromZero),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.ProductName,
                                Description = request.ProductDescription,
                            },
                        },
                    },
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                ClientReferenceId = request.ProductId,
            };

            var service = new SessionService(new StripeClient(_options.SecretKey));
            var session = await service.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            return new PaymentSessionResult { Success = true, CheckoutUrl = session.Url };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe checkout session creation failed");
            return new PaymentSessionResult
            {
                Success = false,
                ErrorMessage = "We couldn't start the test payment with Stripe. Please try again in a moment."
            };
        }
    }

    public async Task<PaymentConfirmation> GetConfirmationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return new PaymentConfirmation { Success = false, ErrorMessage = "Payments aren't configured on the server." };
        }

        try
        {
            var service = new SessionService(new StripeClient(_options.SecretKey));
            var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);

            return new PaymentConfirmation
            {
                Success = session.PaymentStatus == "paid",
                PaymentStatus = session.PaymentStatus,
                CustomerEmail = session.CustomerDetails?.Email,
                AmountTotalCents = session.AmountTotal,
                Currency = session.Currency,
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to retrieve Stripe checkout session {SessionId}", sessionId);
            return new PaymentConfirmation
            {
                Success = false,
                ErrorMessage = "We couldn't confirm this payment with Stripe. If you were charged, contact support with your session ID."
            };
        }
    }
}
