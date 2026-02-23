using System.Diagnostics;

using JosephGuadagno.Broadcasting.Domain;

using Microsoft.AspNetCore.Mvc;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for the home page.
/// </summary>
public class HomeController(ILogger<HomeController> logger) : Controller
{

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