using QuickQuote.Models;

namespace QuickQuote.Services;

public class InMemoryProductCatalog : IProductCatalog
{
    private static readonly IReadOnlyList<Product> Catalog = new List<Product>
    {
        new("starter", "Starter License", "Single-seat access to the core platform — everything a solo builder needs to ship a first project.", 29.00m, "Most flexible"),
        new("pro", "Pro License", "Five-seat team access with priority support and usage analytics built in.", 79.00m, "Most popular"),
        new("business", "Business License", "Twenty-seat access with SSO-ready provisioning and a dedicated onboarding session.", 199.00m, "Best value"),
        new("enterprise", "Enterprise Bundle", "Unlimited seats, custom SLAs, and a named integration engineer for your rollout.", 499.00m, "White glove"),
    };

    public IReadOnlyList<Product> GetAll() => Catalog;

    public Product? GetById(string id) => Catalog.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}
