using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for managing YouTube sources
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
public class YouTubeSourcesController : Controller
{
    private readonly IYouTubeSourceService _youTubeSourceService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor for the YouTubeSourcesController
    /// </summary>
    /// <param name="youTubeSourceService">The YouTube source service</param>
    /// <param name="mapper">The mapper service</param>
    public YouTubeSourcesController(IYouTubeSourceService youTubeSourceService, IMapper mapper)
    {
        _youTubeSourceService = youTubeSourceService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the list of YouTube sources
    /// </summary>
    /// <returns>Returns a List&lt;<see cref="YouTubeSourceViewModel"/>&gt;.</returns>
    public async Task<IActionResult> Index()
    {
        var sources = await _youTubeSourceService.GetAllAsync();
        var viewSources = _mapper.Map<List<YouTubeSourceViewModel>>(sources);
        return View(viewSources);
    }

    /// <summary>
    /// Returns a detail view of the YouTube source
    /// </summary>
    /// <param name="id">The id of the YouTube source</param>
    /// <returns>A <see cref="YouTubeSourceViewModel"/></returns>
    public async Task<IActionResult> Details(int id)
    {
        var source = await _youTubeSourceService.GetAsync(id);
        if (source == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this YouTube source.";
                return RedirectToAction("Index");
            }
        }

        var sourceViewModel = _mapper.Map<YouTubeSourceViewModel>(source);
        return View(sourceViewModel);
    }

    /// <summary>
    /// Adds a new YouTube source
    /// </summary>
    /// <returns>The add new YouTube source view</returns>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public IActionResult Add()
    {
        return View(new YouTubeSourceViewModel { PublicationDate = DateTimeOffset.UtcNow });
    }

    /// <summary>
    /// Adds a new YouTube source
    /// </summary>
    /// <param name="sourceViewModel">The <see cref="YouTubeSourceViewModel"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redirects to the <see cref="Add()"/> page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Add(YouTubeSourceViewModel sourceViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(sourceViewModel);
        }

        var sourceToAdd = _mapper.Map<YouTubeSource>(sourceViewModel);
        sourceToAdd.CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty;
        
        var savedSource = await _youTubeSourceService.SaveAsync(sourceToAdd);
        if (savedSource == null)
        {
            TempData["ErrorMessage"] = "Failed to add the YouTube source.";
            return RedirectToAction("Add");
        }
        
        TempData["SuccessMessage"] = "YouTube source added successfully.";
        return RedirectToAction("Details", new { id = savedSource.Id });
    }

    /// <summary>
    /// Shows the delete confirmation page for a YouTube source
    /// </summary>
    /// <param name="id">The identity of a YouTube source</param>
    /// <returns>The delete confirmation view</returns>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Delete(int id)
    {
        var source = await _youTubeSourceService.GetAsync(id);
        if (source == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this YouTube source.";
                return RedirectToAction("Index");
            }
        }

        var sourceViewModel = _mapper.Map<YouTubeSourceViewModel>(source);
        return View(sourceViewModel);
    }

    /// <summary>
    /// Deletes a YouTube source after confirmation
    /// </summary>
    /// <param name="id">The identity of a YouTube source</param>
    /// <returns>Upon success, redirects to the <see cref="Index"/>. Upon failure, reloads the view.</returns>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var source = await _youTubeSourceService.GetAsync(id);
        if (source == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this YouTube source.";
                return RedirectToAction("Index");
            }
        }

        var result = await _youTubeSourceService.DeleteAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "YouTube source deleted successfully.";
            return RedirectToAction("Index");
        }

        var sourceViewModel = _mapper.Map<YouTubeSourceViewModel>(source);
        ModelState.AddModelError(string.Empty, "Failed to delete the YouTube source.");
        return View(sourceViewModel);
    }
}
