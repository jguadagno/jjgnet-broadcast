using JosephGuadagno.Broadcasting.Domain.Models;
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

    public async Task<IActionResult> Index()
    {
        var scheduledItems = await _scheduledItemService.GetScheduledItemsAsync();
        return View(scheduledItems);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var scheduledItem = await _scheduledItemService.GetScheduledItemAsync(id);
        if (scheduledItem == null)
        {
            return NotFound();
        }
        return View(scheduledItem);
    }
    
    public async Task<IActionResult> Edit(int id)
    {
        var scheduledItem = await _scheduledItemService.GetScheduledItemAsync(id);
        if (scheduledItem == null)
        {
            return NotFound();
        }
        return View(scheduledItem);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ScheduledItem scheduledItem)
    {
        var savedScheduledItem = await _scheduledItemService.SaveScheduledItemAsync(scheduledItem);
        return savedScheduledItem == null
            ? RedirectToAction("Edit", new { id = scheduledItem.Id })
            : RedirectToAction("Details", new { id = savedScheduledItem.Id });
    }
    
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _scheduledItemService.DeleteScheduledItemAsync(id);

        if (result)
        {
            return RedirectToAction("Index");
        }
        return View();
    }
        
    public IActionResult Add()
    {
        return View(new ScheduledItem());
    }
        
    [HttpPost]
    public async Task<RedirectToActionResult> Add(ScheduledItem scheduledItem)
    {
        var savedScheduledItem = await _scheduledItemService.SaveScheduledItemAsync(scheduledItem);
        return savedScheduledItem == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { id = savedScheduledItem.Id });
    }

    [HttpGet("[action]")]
    [HttpGet("[action]/{year:int?}/{month:int?}")]
    public async Task<IActionResult> Calendar(int? year, int? month)
    {
        
        var queryYear = year.HasValue == false ? DateTime.Today.Year : year.Value;
        var queryMonth = month.HasValue == false ? DateTime.Today.Month : month.Value;

        if (year <= 0)
        {
            queryYear = DateTime.MinValue.Year;
        }

        if (month is < 1 or > 12)
        {
            queryMonth = DateTime.Today.Month;
        }

        ViewData["Year"] = queryYear;
        ViewData["Month"] = queryMonth;
        var scheduledItems = await _scheduledItemService.GetScheduledItemsByCalendarMonthAsync(queryYear, queryMonth);
        return View(scheduledItems);
    }

    [HttpGet]
    public async Task<IActionResult> Unsent()
    {
        var scheduledItems = await _scheduledItemService.GetUnsentScheduledItemsAsync();
        return View(scheduledItems);
    }
    
    [HttpGet]
    public async Task<IActionResult> Upcoming()
    {
        var scheduledItems = await _scheduledItemService.GetScheduledItemsToSendAsync();
        return View(scheduledItems);
    }
}


