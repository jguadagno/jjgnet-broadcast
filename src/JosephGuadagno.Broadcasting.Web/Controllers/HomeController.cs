using System.Diagnostics;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

public class HomeController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IEngagementService engagementService,  ILogger<HomeController> logger)
    {
        _engagementService = engagementService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}