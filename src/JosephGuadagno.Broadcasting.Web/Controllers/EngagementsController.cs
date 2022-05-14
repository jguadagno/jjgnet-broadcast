using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

public class EngagementsController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly ILogger<EngagementsController> _logger;

    public EngagementsController(IEngagementService engagementService, ILogger<EngagementsController> logger)
    {
        _engagementService = engagementService;
        _logger = logger;
    }
    
    // GET
    public async Task<IActionResult> Index()
    {
        var engagements = await _engagementService.GetEngagementsAsync();
        return View(engagements);
    }
    // Get One (Details)
    public async Task<IActionResult> Details(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null)
        {
            return NotFound();
        }
        ViewData["Title"] = $"Engagement '{engagement.Name}";
        return View(engagement);
    }
        
    public async Task<IActionResult> Edit(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null)
        {
            return NotFound();
        }
        ViewData["Title"] = $"Edit '{engagement.Name}";
        return View(engagement);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Engagement engagement)
    {
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagement);
        return savedEngagement == null
            ? RedirectToAction("Edit", new { id = engagement.Id })
            : RedirectToAction("Details", new { id = savedEngagement.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _engagementService.DeleteEngagementAsync(id);

        if (result)
        {
            return RedirectToAction("Index");
        }
        return View();
    }
        
    public IActionResult Add()
    {
        return View(new Engagement());
    }
        
    [HttpPost]
    public async Task<RedirectToActionResult> Add(Engagement engagement)
    {
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagement);
        return savedEngagement == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { id = savedEngagement.Id });
    }
    
}