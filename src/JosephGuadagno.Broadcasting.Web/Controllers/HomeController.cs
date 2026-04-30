using System.Diagnostics;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for the home page.
/// </summary>
public class HomeController(ILogger<HomeController> logger) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;

    /// <summary>
    /// Returns the home page
    /// </summary>
    /// <returns>Returns the home page</returns>
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Returns the privacy page
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Returns the authentication error page
    /// </summary>
    /// <param name="message">Optional sanitized error message from the OIDC event handler.</param>
    /// <returns>The authentication error view</returns>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult AuthError(string? message)
    {
        return View(new AuthErrorViewModel
        {
            Message = message ?? "An error occurred during authentication.",
            RetryUrl = "/Account/SignIn"
        });
    }

    /// <summary>
    /// Returns the error page
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}