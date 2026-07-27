using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuickQuote.Models;
using QuickQuote.Services;

namespace QuickQuote.Controllers;

public class HomeController : Controller
{
    private readonly IProductCatalog _catalog;

    public HomeController(IProductCatalog catalog)
    {
        _catalog = catalog;
    }

    public IActionResult Index()
    {
        return View(_catalog.GetAll());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
