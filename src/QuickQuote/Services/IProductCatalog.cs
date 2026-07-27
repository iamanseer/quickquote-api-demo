using QuickQuote.Models;

namespace QuickQuote.Services;

public interface IProductCatalog
{
    IReadOnlyList<Product> GetAll();
    Product? GetById(string id);
}
