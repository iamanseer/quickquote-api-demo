using Microsoft.AspNetCore.Mvc;
using QuickQuote.Models;
using QuickQuote.Services;

namespace QuickQuote.Controllers;

public class CheckoutController : Controller
{
    private static readonly string[] DisplayCurrencies = { "EUR", "GBP", "INR" };

    private readonly IProductCatalog _catalog;
    private readonly IExchangeRateService _exchangeRates;
    private readonly IPaymentService _payments;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        IProductCatalog catalog,
        IExchangeRateService exchangeRates,
        IPaymentService payments,
        ILogger<CheckoutController> logger)
    {
        _catalog = catalog;
        _exchangeRates = exchangeRates;
        _payments = payments;
        _logger = logger;
    }

    [HttpGet("Checkout/{productId}")]
    public async Task<IActionResult> Index(string productId, bool cancelled, CancellationToken cancellationToken)
    {
        var product = _catalog.GetById(productId);
        if (product is null)
        {
            return NotFound();
        }

        var rates = await _exchangeRates.ConvertAsync(product.PriceUsd, DisplayCurrencies, cancellationToken);

        var viewModel = new CheckoutViewModel
        {
            Product = product,
            ExchangeRates = rates,
            PaymentError = TempData["PaymentError"] as string,
            PaymentCancelled = cancelled,
        };

        return View(viewModel);
    }

    [HttpPost("Checkout/{productId}/pay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(string productId, CancellationToken cancellationToken)
    {
        var product = _catalog.GetById(productId);
        if (product is null)
        {
            return NotFound();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var request = new PaymentSessionRequest(
            ProductId: product.Id,
            ProductName: product.Name,
            ProductDescription: product.Description,
            AmountUsd: product.PriceUsd,
            SuccessUrl: $"{baseUrl}/Checkout/Success?session_id={{CHECKOUT_SESSION_ID}}&productId={product.Id}",
            CancelUrl: $"{baseUrl}/Checkout/{product.Id}?cancelled=true");

        var result = await _payments.CreateCheckoutSessionAsync(request, cancellationToken);

        if (!result.Success || string.IsNullOrEmpty(result.CheckoutUrl))
        {
            _logger.LogWarning("Stripe checkout session could not be created for product {ProductId}: {Error}", productId, result.ErrorMessage);
            TempData["PaymentError"] = result.ErrorMessage ?? "Something went wrong starting the test payment. Please try again.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        return Redirect(result.CheckoutUrl);
    }

    [HttpGet("Checkout/Success")]
    public async Task<IActionResult> Success(string session_id, string productId, CancellationToken cancellationToken)
    {
        var product = _catalog.GetById(productId);
        if (product is null || string.IsNullOrWhiteSpace(session_id))
        {
            return NotFound();
        }

        var confirmation = await _payments.GetConfirmationAsync(session_id, cancellationToken);

        return View(new ConfirmationViewModel { Product = product, Payment = confirmation });
    }
}
