# QuickQuote

QuickQuote is a small ASP.NET Core MVC checkout flow that integrates two real
third-party APIs: a live currency-conversion service and Stripe Checkout in
test mode. It's a portfolio piece built to demonstrate clean third-party API
integration — not a production storefront.

**Live demo:** http://order-flow.runasp.net _(placeholder — update once deployed)_

## What it does

- **Plan catalog** — four seeded SaaS-style license plans, each with a USD price.
- **Checkout page** — for the selected plan, it calls the [Frankfurter](https://frankfurter.dev)
  exchange-rate API live, on every page load, and shows the price converted into
  EUR, GBP, and INR alongside the USD price.
- **Payment** — clicking "Pay with Stripe" creates a real Stripe Checkout Session
  (test mode) via the Stripe API and redirects the buyer to Stripe's own hosted
  payment page. No card data ever touches this app.
- **Confirmation page** — after a successful test payment, Stripe redirects back
  and the app looks up the session server-side to show a real order summary
  (amount charged, currency, payment status, receipt email).
- **Error handling** —
  - If the exchange-rate API is unreachable or errors, the checkout page falls
    back to USD-only pricing with a visible notice, instead of failing the page.
  - If Stripe can't create a session (misconfigured key, API error), the user is
    sent back to checkout with a clear error message and nothing is charged.
  - If a buyer cancels on Stripe's page, they land back on checkout with a
    "no charge was made" notice.

## Tech stack

- **ASP.NET Core MVC on .NET 8**
- **Stripe.net** for the Stripe Checkout Session API (test mode)
- **Frankfurter API** (`api.frankfurter.dev`) for live FX rates — free, no API
  key required, which is why it was chosen over a keyed provider for a demo
- Server-rendered Razor views, hand-built CSS (no UI framework), "Space Grotesk"
  + "Inter" from Google Fonts

### Architecture: the service layer

The point of this project is the integration work, so the two third-party
integrations are isolated behind interfaces in `Services/`, and controllers
depend only on those interfaces — never on `HttpClient`, the Stripe SDK, or any
provider-specific type directly:

```
Controllers/CheckoutController.cs
    depends on -> IExchangeRateService   (Services/IExchangeRateService.cs)
    depends on -> IPaymentService        (Services/IPaymentService.cs)

Services/FrankfurterExchangeRateService.cs   implements IExchangeRateService
Services/StripePaymentService.cs             implements IPaymentService
```

Both implementations return a typed result object (`ExchangeRateResult`,
`PaymentSessionResult`, `PaymentConfirmation`) with a `Success` flag and an
`ErrorMessage`, rather than throwing on expected failure modes (API down,
payment declined, session not found). Controllers branch on `Success` and pass
the result straight to the view — there's no try/catch scattered through the
controller, and no HTTP or Stripe-specific exception type leaks past the
service boundary.

Why this matters for client work: a client's payment provider or FX provider
is one of the most likely things to change over the life of a project — a
cheaper gateway, a provider outage, a move from test to a different sandbox.
With the provider-specific code contained to one class behind an interface,
that swap (or a unit test with a fake implementation) doesn't touch a single
controller or view.

## Running it locally

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A free [Stripe](https://dashboard.stripe.com/register) account (no card
  required) for **test-mode** API keys

### 1. Get a Stripe test secret key

1. Sign up / log in at [dashboard.stripe.com](https://dashboard.stripe.com).
2. Make sure the dashboard is in **Test mode** (toggle, top right).
3. Go to **Developers → API keys** and copy the **Secret key** (starts with
   `sk_test_...`).

### 2. Configure the key with user secrets (never in appsettings.json)

```bash
cd src/QuickQuote
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_key_here"
```

`appsettings.Example.json` shows the shape of the config value if you'd rather
set it as an environment variable (`Stripe__SecretKey`) instead.

### 3. Run it

```bash
dotnet run --project src/QuickQuote
```

Then open the URL printed in the console (typically `https://localhost:5001`).

### 4. Test a payment

On Stripe's hosted checkout page, use any of
[Stripe's test cards](https://stripe.com/docs/testing) — e.g. card number
`4242 4242 4242 4242`, any future expiry date, any 3-digit CVC, any postal code.
No real charge is ever made in test mode.

## No secrets are committed

- `appsettings.json` only ever contains an **empty** placeholder for
  `Stripe:SecretKey` — safe to commit, nothing to leak.
- `appsettings.Example.json` documents the config shape without a real value.
- `.gitignore` excludes `appsettings.Development.json` / `appsettings.Production.json`
  and `.env*` files as a second layer of protection, in case a real key ever
  ends up in one of those during local development.
- The real key only ever lives in `dotnet user-secrets` locally (stored outside
  the repo, under your user profile) or in the hosting provider's environment
  variables in production.

## Deployment notes

Deployed to shared hosting (MonsterASP.NET) via FTP as a self-contained
publish. Stripe test mode doesn't require any special configuration for the
live domain — test-mode keys work from any origin, since Checkout Sessions are
created server-side and the buyer is redirected to `checkout.stripe.com`, not
to a webhook or redirect URL that Stripe needs to pre-register. The only thing
that matters is that `Stripe:SecretKey` is set as an environment variable (or
equivalent) on the host, exactly as it is locally via user secrets.

## Screenshots

_Add screenshots here after deploying — plan catalog, checkout page with live
FX rates, Stripe's hosted payment page, and the confirmation page._
