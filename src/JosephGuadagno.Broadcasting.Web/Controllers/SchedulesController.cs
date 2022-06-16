using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for the schedules
/// </summary>
public class SchedulesController : Controller
{
    private readonly IScheduledItemService _scheduledItemService;
    private readonly ILogger<SchedulesController> _logger;

    /// <summary>
    /// The constructor for the schedules controller
    /// </summary>
    /// <param name="scheduledItemService">The scheduled item service</param>
    /// <param name="logger">The logger to use</param>
    public SchedulesController(IScheduledItemService scheduledItemService, ILogger<SchedulesController> logger)
    {
        _scheduledItemService = scheduledItemService;
        _logger = logger;
    }

    /// <summary>
    /// The list of schedules
    /// </summary>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;</returns>
    public async Task<IActionResult> Index()
    {
        var scheduledItems = await _scheduledItemService.GetScheduledItemsAsync();
        return View(scheduledItems);
    }
    
    /// <summary>
    /// Details for a specific schedule
    /// </summary>
    /// <param name="id">The id of the specific schedule</param>
    /// <returns>A <see cref="ScheduledItem"/></returns>
    public async Task<IActionResult> Details(int id)
    {
        var scheduledItem = await _scheduledItemService.GetScheduledItemAsync(id);
        if (scheduledItem == null)
        {
            return NotFound();
        }
        return View(scheduledItem);
    }
    
    /// <summary>
    /// Edits a scheduled item
    /// </summary>
    /// <param name="id">The id of the scheduled item</param>
    /// <returns>Returns an editable scheduled item</returns>
    public async Task<IActionResult> Edit(int id)
    {
        var scheduledItem = await _scheduledItemService.GetScheduledItemAsync(id);
        if (scheduledItem == null)
        {
            return NotFound();
        }
        return View(scheduledItem);
    }

    /// <summary>
    /// Saves the updates to a scheduled item
    /// </summary>
    /// <param name="scheduledItem">The <see cref="ScheduledItem"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, reloads the page.</returns>
    [HttpPost]
    public async Task<IActionResult> Edit(ScheduledItem scheduledItem)
    {
        var savedScheduledItem = await _scheduledItemService.SaveScheduledItemAsync(scheduledItem);
        return savedScheduledItem == null
            ? RedirectToAction("Edit", new { id = scheduledItem.Id })
            : RedirectToAction("Details", new { id = savedScheduledItem.Id });
    }
    
    /// <summary>
    /// Deletes the scheduled item
    /// </summary>
    /// <param name="id">The id of the scheduled item</param>
    /// <returns>Upon success, redirects to the <see cref="Index"/>. Upon failure, returns the error.</returns>
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
    
    /// <summary>
    /// Adds a new scheduled item
    /// </summary>
    /// <returns>Returns a form to add a <see cref="ScheduledItem"/></returns>
    public IActionResult Add()
    {
        return View(new ScheduledItem());
    }
        
    /// <summary>
    /// Adds a new scheduled item
    /// </summary>
    /// <param name="scheduledItem">The <see cref="ScheduledItem"/> to be added</param>
    /// <returns>Upon success, redirects to the <see cref="Details"/>. Upon failure, reloads the page</returns>
    [HttpPost]
    public async Task<RedirectToActionResult> Add(ScheduledItem scheduledItem)
    {
        var savedScheduledItem = await _scheduledItemService.SaveScheduledItemAsync(scheduledItem);
        return savedScheduledItem == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { id = savedScheduledItem.Id });
    }

    /// <summary>
    /// Returns a calendar view of scheduled items
    /// </summary>
    /// <param name="year">A filter for the year</param>
    /// <param name="month">A filter for the month</param>
    /// <returns>A calendar to view all of the scheduled items</returns>
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

    /// <summary>
    /// Returns a view with all of the scheduled items that have not been sent.
    /// </summary>
    /// <returns>A view of unsent scheduled items</returns>
    [HttpGet]
    public async Task<IActionResult> Unsent()
    {
        var scheduledItems = await _scheduledItemService.GetUnsentScheduledItemsAsync();
        return View(scheduledItems);
    }
    
    /// <summary>
    /// Returns a view with all of the upcoming items that need to be sent.
    /// </summary>
    /// <returns>A view of upcoming scheduled items</returns>
    [HttpGet]
    public async Task<IActionResult> Upcoming()
    {
        var scheduledItems = await _scheduledItemService.GetScheduledItemsToSendAsync();
        return View(scheduledItems);
    }
}