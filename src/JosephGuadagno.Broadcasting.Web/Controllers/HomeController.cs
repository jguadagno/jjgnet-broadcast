using System.Diagnostics;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

public class HomeController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly IScheduledItemService _scheduledItemService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IEngagementService engagementService, IScheduledItemService scheduledItemService, ILogger<HomeController> logger)
    {
        _engagementService = engagementService;
        _scheduledItemService = scheduledItemService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var engagements = await _engagementService.GetEngagementsAsync();
        var engagement = await _engagementService.GetEngagementAsync(1);
        var talks = await _engagementService.GetEngagementTalksAsync(1);
        var talk = await _engagementService.GetEngagementTalkAsync(1, 2);

        var scheduledItems = await _scheduledItemService.GetScheduledItemsAsync();
        var scheduledItem = await _scheduledItemService.GetScheduledItemAsync(1);
        var upcomingItems = await _scheduledItemService.GetUpcomingScheduledItems();
        
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