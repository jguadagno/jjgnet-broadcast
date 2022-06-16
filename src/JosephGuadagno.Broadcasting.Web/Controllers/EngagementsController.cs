using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// This is the controller for managing the engagements.
/// </summary>
public class EngagementsController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly ILogger<EngagementsController> _logger;

    /// <summary>
    /// The constructor for the EngagementsController.
    /// </summary>
    /// <param name="engagementService">The engagement service</param>
    /// <param name="logger">The logger to use</param>
    public EngagementsController(IEngagementService engagementService, ILogger<EngagementsController> logger)
    {
        _engagementService = engagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of engagements. 
    /// </summary>
    /// <returns>Returns a List&lt;<see cref="Engagement"/>&gt;.</returns>
    public async Task<IActionResult> Index()
    {
        var engagements = await _engagementService.GetEngagementsAsync();
        return View(engagements);
    }

    /// <summary>
    /// Returns a detail view of the engagement.
    /// </summary>
    /// <param name="id">The id of the engagement</param>
    /// <returns>An <see cref="Engagement"/></returns>
    public async Task<IActionResult> Details(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null)
        {
            return NotFound();
        }

        return View(engagement);
    }

    /// <summary>
    /// Edits an engagement.
    /// </summary>
    /// <param name="id">The id of the engagement</param>
    /// <returns>An <see cref="Engagement"/></returns>
    public async Task<IActionResult> Edit(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null)
        {
            return NotFound();
        }

        return View(engagement);
    }

    /// <summary>
    /// Applies the changes to the engagement.
    /// </summary>
    /// <param name="engagement">The <see cref="Engagement"/> to edit.</param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redirects to the <see cref="Edit(int)"/> page.</returns>
    [HttpPost]
    public async Task<IActionResult> Edit(Engagement engagement)
    {
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagement);
        return savedEngagement == null
            ? RedirectToAction("Edit", new { id = engagement.Id })
            : RedirectToAction("Details", new { id = savedEngagement.Id });
    }

    /// <summary>
    /// Deletes an engagement.
    /// </summary>
    /// <param name="id">The identity of an engagement</param>
    /// <returns>Upon success, redirects to the <see cref="Index"/></returns>
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

    /// <summary>
    /// Adds a new engagement.
    /// </summary>
    /// <returns>The add new engagement view.</returns>
    public IActionResult Add()
    {
        return View(new Engagement());
    }

    /// <summary>
    /// Adds a new engagement.
    /// </summary>
    /// <param name="engagement">The <see cref="Engagement"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redisplays the team. </returns>
    [HttpPost]
    public async Task<RedirectToActionResult> Add(Engagement engagement)
    {
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagement);
        return savedEngagement == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { id = savedEngagement.Id });
    }
}