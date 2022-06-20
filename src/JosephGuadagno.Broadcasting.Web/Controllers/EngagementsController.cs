using AutoMapper;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// This is the controller for managing the engagements.
/// </summary>
public class EngagementsController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly IMapper _mapper;
    private readonly ILogger<EngagementsController> _logger;

    /// <summary>
    /// The constructor for the EngagementsController.
    /// </summary>
    /// <param name="engagementService">The engagement service</param>
    /// <param name="mapper">The mapper service</param>
    /// <param name="logger">The logger to use</param>
    public EngagementsController(IEngagementService engagementService, IMapper mapper, ILogger<EngagementsController> logger)
    {
        _engagementService = engagementService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of engagements. 
    /// </summary>
    /// <returns>Returns a List&lt;<see cref="EngagementViewModel"/>&gt;.</returns>
    public async Task<IActionResult> Index()
    {
        var engagements = await _engagementService.GetEngagementsAsync();
        var viewEngagements = _mapper.Map<List<EngagementViewModel>>(engagements);
        return View(viewEngagements);
    }

    /// <summary>
    /// Returns a detail view of the engagement.
    /// </summary>
    /// <param name="id">The id of the engagement</param>
    /// <returns>An <see cref="EngagementViewModel"/></returns>
    public async Task<IActionResult> Details(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null)
        {
            return NotFound();
        }

        var engagementViewModel = _mapper.Map<EngagementViewModel>(engagement);
        return View(engagementViewModel);
    }

    /// <summary>
    /// Edits an engagement.
    /// </summary>
    /// <param name="id">The id of the engagement</param>
    /// <returns>An <see cref="EngagementViewModel"/></returns>
    public async Task<IActionResult> Edit(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null)
        {
            return NotFound();
        }

        var engagementViewModel = _mapper.Map<EngagementViewModel>(engagement);
        return View(engagementViewModel);
    }

    /// <summary>
    /// Applies the changes to the engagement.
    /// </summary>
    /// <param name="engagementViewModel">The <see cref="EngagementViewModel"/> to edit.</param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redirects to the <see cref="Edit(int)"/> page.</returns>
    [HttpPost]
    public async Task<IActionResult> Edit(EngagementViewModel engagementViewModel)
    {
        var engagementToEdit = _mapper.Map<Domain.Models.Engagement>(engagementViewModel);
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagementToEdit);
        return savedEngagement == null
            ? RedirectToAction("Edit", new { id = engagementViewModel.Id })
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
        return View(new EngagementViewModel());
    }

    /// <summary>
    /// Adds a new engagement.
    /// </summary>
    /// <param name="engagementViewModel">The <see cref="EngagementViewModel"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redisplays the team. </returns>
    [HttpPost]
    public async Task<RedirectToActionResult> Add(EngagementViewModel engagementViewModel)
    {
        var engagementToAdd = _mapper.Map<Domain.Models.Engagement>(engagementViewModel);
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagementToAdd);
        return savedEngagement == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { id = savedEngagement.Id });
    }
}