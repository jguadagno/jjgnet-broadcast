using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for the home page.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    /// <summary>
    /// The constructor for the home controller.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the home page
    /// </summary>
    /// <returns>Returns the home page</returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Returns the privacy page
    /// </summary>
    /// <returns></returns>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Returns the error page
    /// </summary>
    /// <returns></returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}