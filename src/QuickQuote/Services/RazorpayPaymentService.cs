using Microsoft.Extensions.Options;
using QuickQuote.Models;
using Razorpay.Api;
using Razorpay.Api.Errors;

namespace QuickQuote.Services;

/// <summary>
/// Razorpay integration, running against Razorpay's test-mode API via Payment
/// Links (a server-created, hosted, redirect-based payment page — the closest
/// Razorpay equivalent to Stripe Checkout Sessions). The key pair is read from
/// configuration (user secrets locally, environment variables in production)
/// and is never hardcoded or committed.
/// </summary>
public class RazorpayPaymentService : IPaymentService
{
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayPaymentService> _logger;

    public RazorpayPaymentService(IOptions<RazorpayOptions> options, ILogger<RazorpayPaymentService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<PaymentSessionResult> CreateCheckoutSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.KeySecret))
        {
            _logger.LogWarning("Razorpay key pair is not configured; cannot create a payment link");
            return Task.FromResult(new PaymentSessionResult
            {
                Success = false,
                ErrorMessage = "Payments aren't configured yet — a Razorpay test key pair is missing on the server.",
            });
        }

        try
        {
            var client = new RazorpayClient(_options.KeyId, _options.KeySecret);

            var data = new Dictionary<string, object>
            {
                ["amount"] = (long)Math.Round(request.AmountInr * 100m, MidpointRounding.AwayFromZero),
                ["currency"] = "INR",
                ["description"] = request.ProductDescription,
                ["reference_id"] = Guid.NewGuid().ToString("N"),
                ["callback_url"] = request.CallbackUrl,
                ["callback_method"] = "get",
                ["notes"] = new Dictionary<string, object> { ["product_id"] = request.ProductId },
            };

            var paymentLink = client.PaymentLink.Create(data);
            string checkoutUrl = (string)paymentLink["short_url"];

            return Task.FromResult(new PaymentSessionResult { Success = true, CheckoutUrl = checkoutUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay payment link creation failed");
            return Task.FromResult(new PaymentSessionResult
            {
                Success = false,
                ErrorMessage = "We couldn't start the test payment with Razorpay. Please try again in a moment.",
            });
        }
    }

    public Task<PaymentConfirmation> ConfirmAsync(IReadOnlyDictionary<string, string> callbackParameters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.KeySecret))
        {
            return Task.FromResult(new PaymentConfirmation { Success = false, ErrorMessage = "Payments aren't configured on the server." });
        }

        if (!callbackParameters.TryGetValue("razorpay_payment_id", out var paymentId) || string.IsNullOrWhiteSpace(paymentId))
        {
            return Task.FromResult(new PaymentConfirmation { Success = false, ErrorMessage = "No payment reference was returned by Razorpay." });
        }

        try
        {
            // RazorpayClient.Secret is a process-wide static that both the signature
            // verification below and Payment.Fetch read internally, so the client only
            // needs to be constructed once to set it.
            var client = new RazorpayClient(_options.KeyId, _options.KeySecret);

            Utils.verifyPaymentLinkSignature(new Dictionary<string, string>
            {
                ["razorpay_signature"] = callbackParameters.GetValueOrDefault("razorpay_signature", string.Empty),
                ["payment_link_status"] = callbackParameters.GetValueOrDefault("razorpay_payment_link_status", string.Empty),
                ["payment_link_id"] = callbackParameters.GetValueOrDefault("razorpay_payment_link_id", string.Empty),
                ["payment_link_reference_id"] = callbackParameters.GetValueOrDefault("razorpay_payment_link_reference_id", string.Empty),
                ["razorpay_payment_id"] = paymentId,
            });

            var payment = client.Payment.Fetch(paymentId);

            string status = (string)payment["status"];

            return Task.FromResult(new PaymentConfirmation
            {
                Success = status is "captured" or "authorized",
                PaymentStatus = status,
                CustomerEmail = (string?)payment["email"],
                AmountTotalMinorUnits = (long)payment["amount"],
                Currency = (string)payment["currency"],
            });
        }
        catch (SignatureVerificationError ex)
        {
            _logger.LogWarning(ex, "Razorpay payment link signature verification failed");
            return Task.FromResult(new PaymentConfirmation
            {
                Success = false,
                ErrorMessage = "We couldn't verify this payment with Razorpay. If you were charged, contact support with your payment ID.",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm Razorpay payment {PaymentId}", paymentId);
            return Task.FromResult(new PaymentConfirmation
            {
                Success = false,
                ErrorMessage = "We couldn't confirm this payment with Razorpay. If you were charged, contact support with your payment ID.",
            });
        }
    }
}
