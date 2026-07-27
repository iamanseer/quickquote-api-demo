# Case study: QuickQuote — checkout flow with live FX rates + Stripe

For this project I built a checkout flow in ASP.NET Core MVC that integrates
two real third-party APIs: a live currency-conversion service and Stripe
Checkout running in test mode. The goal wasn't a full storefront — it was to
show, end to end, how I handle bringing an external API into a client's app
without it becoming a liability.

On the checkout page, the product price is converted live into three
currencies by calling a foreign-exchange API on every page load — no cached
or hardcoded rates. The actual payment step creates a real Stripe Checkout
Session server-side and redirects the buyer to Stripe's own hosted page, so
card data never touches my code. After payment, the confirmation page looks
the session back up through Stripe's API to show a real order summary, not a
static "thank you" message.

The part I'd point to as the real deliverable is the service layer. Both
integrations sit behind interfaces — `IExchangeRateService` and
`IPaymentService` — and every controller talks to those interfaces only, never
to `HttpClient` or the Stripe SDK directly. Each service returns a typed
result with a success flag and an error message instead of throwing on
expected failures, so a dead FX API or a declined test payment degrades
gracefully — the checkout page falls back to USD pricing with a visible
notice, and a failed Stripe session sends the buyer back with a clear message
instead of a stack trace.

This pattern is what I bring to client work. Payment gateways and data
providers change — a client swaps processors, a sandbox API goes down, a
provider gets replaced for pricing reasons — and when that logic is isolated
behind an interface, that change is a new class, not a rewrite of every
controller and view that touched it. It also means the integration can be
unit tested with a fake implementation, without ever hitting a real API in CI.
That's the difference between an integration that's convenient today and one
that's maintainable in a year.
