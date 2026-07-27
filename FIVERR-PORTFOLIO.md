# Case study: QuickQuote — checkout flow with live FX rates + Razorpay

For this project I built a checkout flow in ASP.NET Core MVC that integrates
two real third-party APIs: a live currency-conversion service and Razorpay's
hosted checkout in test mode. The goal wasn't a full storefront — it was to
show, end to end, how I handle bringing an external API into a client's app
without it becoming a liability.

On the checkout page, the product price is converted live into three
currencies by calling a foreign-exchange API on every page load — no cached
or hardcoded rates. The payment step goes further: it fetches a fresh
USD-to-INR rate at the moment of payment and charges that live amount through
a real Razorpay Payment Link, redirecting the buyer to Razorpay's own hosted
page so card data never touches my code. After payment, the confirmation page
verifies Razorpay's signed callback and looks the payment back up through its
API to show a real order summary, not a static "thank you" message.

The part I'd point to as the real deliverable is the service layer. Both
integrations sit behind interfaces — `IExchangeRateService` and
`IPaymentService` — and every controller talks to those interfaces only,
never to a provider SDK directly. That design got tested for real mid-build:
the project started against Stripe, until I hit Stripe's India signup
restriction, which blocks even test-mode API keys, not just live payouts.
Swapping to Razorpay meant writing one new class behind the existing
interface — zero changes to any controller, view, or model outside the
service layer.

This pattern is what I bring to client work. Payment gateways and data
providers change — a client swaps processors, a sandbox goes down, a provider
gets restricted in a region — and when that logic sits behind an interface,
the change is a new class, not a rewrite of every controller and view that
touched it. It also means the integration can be unit tested with a fake
implementation, without hitting a real API in CI. That's the difference
between an integration that's convenient today and one that's maintainable
in a year.
