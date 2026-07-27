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
    public async Task<IActionResult> Index(string productId, CancellationToken cancellationToken)
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

        // Razorpay settles in INR, so the amount actually charged is the live-converted
        // INR price, not a fixed number — the FX integration isn't just decorative here.
        var inrRate = await _exchangeRates.ConvertAsync(product.PriceUsd, new[] { "INR" }, cancellationToken);
        var inrQuote = inrRate.Quotes.FirstOrDefault(q => q.Currency == "INR");

        if (!inrRate.Success || inrQuote is null)
        {
            _logger.LogWarning("Could not resolve a live INR price for product {ProductId}: {Error}", productId, inrRate.ErrorMessage);
            TempData["PaymentError"] = "We couldn't fetch a live price to charge right now. Please try again in a moment.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var request = new PaymentSessionRequest(
            ProductId: product.Id,
            ProductName: product.Name,
            ProductDescription: product.Description,
            AmountInr: inrQuote.ConvertedAmount,
            CallbackUrl: $"{baseUrl}/Checkout/Success?productId={product.Id}");

        var result = await _payments.CreateCheckoutSessionAsync(request, cancellationToken);

        if (!result.Success || string.IsNullOrEmpty(result.CheckoutUrl))
        {
            _logger.LogWarning("Razorpay payment link could not be created for product {ProductId}: {Error}", productId, result.ErrorMessage);
            TempData["PaymentError"] = result.ErrorMessage ?? "Something went wrong starting the test payment. Please try again.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        return Redirect(result.CheckoutUrl);
    }

    [HttpGet("Checkout/Success")]
    public async Task<IActionResult> Success(string productId, CancellationToken cancellationToken)
    {
        var product = _catalog.GetById(productId);
        if (product is null)
        {
            return NotFound();
        }

        var callbackParameters = Request.Query
            .Where(q => q.Key != "productId")
            .ToDictionary(q => q.Key, q => q.Value.ToString());

        var confirmation = await _payments.ConfirmAsync(callbackParameters, cancellationToken);

        return View(new ConfirmationViewModel { Product = product, Payment = confirmation });
    }
}
