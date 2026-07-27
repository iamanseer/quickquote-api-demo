# QuickQuote

QuickQuote is a small ASP.NET Core MVC checkout flow that integrates two real
third-party APIs: a live currency-conversion service and Razorpay's hosted
checkout in test mode. It's a portfolio piece built to demonstrate clean
third-party API integration — not a production storefront.

**Live demo:** http://quick-quote.runasp.net

## What it does

- **Plan catalog** — four seeded SaaS-style license plans, each with a USD price.
- **Checkout page** — for the selected plan, it calls the [Frankfurter](https://frankfurter.dev)
  exchange-rate API live, on every page load, and shows the price converted into
  EUR, GBP, and INR alongside the USD price.
- **Payment** — clicking "Pay with Razorpay" fetches a fresh live USD→INR rate,
  then creates a real Razorpay Payment Link (test mode) via the Razorpay API for
  that rupee amount and redirects the buyer to Razorpay's own hosted payment
  page. No card or UPI data ever touches this app. The FX integration isn't
  just decorative — it's what determines the amount actually charged.
- **Confirmation page** — after a successful test payment, Razorpay redirects
  back with a signed callback. The app verifies that signature server-side,
  looks the payment up through Razorpay's API, and shows a real order summary
  (amount charged, currency, payment status, receipt email).
- **Error handling** —
  - If the exchange-rate API is unreachable or errors, the checkout page falls
    back to USD-only pricing with a visible notice, instead of failing the page.
  - If a live rate can't be fetched at the moment of payment, checkout is
    blocked with a clear message rather than charging a stale or wrong amount.
  - If Razorpay can't create a payment link (misconfigured key, API error), the
    user is sent back to checkout with a clear error message and nothing is
    charged.
  - If the callback signature doesn't verify, the confirmation page reports
    that the payment couldn't be confirmed instead of assuming success.

## Tech stack

- **ASP.NET Core MVC on .NET 8**
- **Razorpay's official .NET SDK** for the Payment Links API (test mode)
- **Frankfurter API** (`api.frankfurter.dev`) for live FX rates — free, no API
  key required, which is why it was chosen over a keyed provider for a demo
- Server-rendered Razor views, hand-built CSS (no UI framework), "Space Grotesk"
  + "Inter" from Google Fonts

### Why Razorpay and not Stripe

This project originally integrated Stripe Checkout, per the original brief.
Stripe closed self-serve account creation for India-based signups in 2024 —
new accounts need an invitation, which blocks reaching even **test-mode** API
keys, not just live payouts. Razorpay was the fallback: self-serve signup,
test-mode keys immediately with zero KYC, and an actively-maintained official
.NET SDK. Swapping the provider meant writing one new class
(`RazorpayPaymentService`) behind the existing `IPaymentService` interface —
no controller, view, or model outside the service layer changed. That's not
an accident; it's the point of the architecture below.

### Architecture: the service layer

The point of this project is the integration work, so the two third-party
integrations are isolated behind interfaces in `Services/`, and controllers
depend only on those interfaces — never on `HttpClient`, the Razorpay SDK, or
any provider-specific type directly:

```
Controllers/CheckoutController.cs
    depends on -> IExchangeRateService   (Services/IExchangeRateService.cs)
    depends on -> IPaymentService        (Services/IPaymentService.cs)

Services/FrankfurterExchangeRateService.cs   implements IExchangeRateService
Services/RazorpayPaymentService.cs           implements IPaymentService
```

Both implementations return a typed result object (`ExchangeRateResult`,
`PaymentSessionResult`, `PaymentConfirmation`) with a `Success` flag and an
`ErrorMessage`, rather than throwing on expected failure modes (API down,
payment declined, signature invalid). Controllers branch on `Success` and pass
the result straight to the view — there's no try/catch scattered through the
controller, and no HTTP or Razorpay-specific exception type leaks past the
service boundary.

Why this matters for client work: a client's payment provider or FX provider
is one of the most likely things to change over the life of a project — a
cheaper gateway, a provider outage, a regional restriction (as happened here),
a move from test to a different sandbox. With the provider-specific code
contained to one class behind an interface, that swap (or a unit test with a
fake implementation) doesn't touch a single controller or view.

## Running it locally

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A free [Razorpay](https://dashboard.razorpay.com/signup) account (email/phone
  signup, no KYC needed) for **test-mode** API keys

### 1. Get a Razorpay test key pair

1. Sign up / log in at [dashboard.razorpay.com](https://dashboard.razorpay.com).
2. Make sure the dashboard is in **Test Mode** (toggle, top left).
3. Go to **Settings → API Keys → Generate Test Key** and copy the **Key Id**
   (`rzp_test_...`) and **Key Secret** shown.

### 2. Configure the keys with user secrets (never in appsettings.json)

```bash
cd src/QuickQuote
dotnet user-secrets init
dotnet user-secrets set "Razorpay:KeyId" "rzp_test_your_key_id"
dotnet user-secrets set "Razorpay:KeySecret" "your_key_secret"
```

`appsettings.Example.json` shows the shape of the config value if you'd rather
set these as environment variables (`Razorpay__KeyId`, `Razorpay__KeySecret`)
instead.

### 3. Run it

```bash
dotnet run --project src/QuickQuote
```

Then open the URL printed in the console (typically `https://localhost:5001`).

### 4. Test a payment

On Razorpay's hosted payment page, use a
[Razorpay test card](https://razorpay.com/docs/payments/payments/test-card-upi-details/)
— e.g. card number `4111 1111 1111 1111`, any future expiry date, any 3-digit
CVV — or the test UPI ID `success@razorpay`. No real charge is ever made in
test mode.

## No secrets are committed

- `appsettings.json` only ever contains **empty** placeholders for
  `Razorpay:KeyId` / `Razorpay:KeySecret` — safe to commit, nothing to leak.
- `appsettings.Example.json` documents the config shape without real values.
- `.gitignore` excludes `appsettings.Development.json` / `appsettings.Production.json`
  and `.env*` files as a second layer of protection, in case real keys ever
  end up in one of those during local development.
- The real keys only ever live in `dotnet user-secrets` locally (stored
  outside the repo, under your user profile) or in the hosting provider's
  environment variables in production.

## Deployment notes

Deployed to shared hosting (MonsterASP.NET) via FTP as a self-contained
publish. Razorpay test mode doesn't require any special configuration for the
live domain — test-mode keys work from any origin, since payment links are
created server-side and the buyer is redirected to Razorpay's hosted page,
with the result delivered back via a signed query-string callback rather than
a pre-registered webhook. The only thing that matters is that
`Razorpay:KeyId` / `Razorpay:KeySecret` are set as environment variables (or
equivalent) on the host, exactly as they are locally via user secrets.

## Screenshots

_Add screenshots here after deploying — plan catalog, checkout page with live
FX rates, Razorpay's hosted payment page, and the confirmation page._
