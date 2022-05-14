using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

public class SchedulesController : Controller
{
    private readonly IScheduledItemService _scheduledItemService;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(IScheduledItemService scheduledItemService, ILogger<SchedulesController> logger)
    {
        _scheduledItemService = scheduledItemService;
        _logger = logger;
    }
    
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
}