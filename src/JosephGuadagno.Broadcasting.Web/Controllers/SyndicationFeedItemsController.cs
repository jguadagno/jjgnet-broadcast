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
/// Controller for managing syndication feed sources
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
public class SyndicationFeedItemsController : Controller
{
    private readonly ISyndicationFeedItemService _syndicationFeedItemService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor for the SyndicationFeedItemsController
    /// </summary>
    /// <param name="syndicationFeedItemService">The syndication feed source service</param>
    /// <param name="mapper">The mapper service</param>
    public SyndicationFeedItemsController(ISyndicationFeedItemService syndicationFeedItemService, IMapper mapper)
    {
        _syndicationFeedItemService = syndicationFeedItemService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the list of syndication feed sources
    /// </summary>
    /// <returns>Returns a List&lt;<see cref="SyndicationFeedItemViewModel"/>&gt;.</returns>
    public async Task<IActionResult> Index(int page = Pagination.DefaultPage, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        var result = await _syndicationFeedItemService.GetAllAsync(page, Pagination.DefaultPageSize, sortBy, sortDescending, filter);
        var viewSources = _mapper.Map<List<SyndicationFeedItemViewModel>>(result.Items);

        ViewBag.Page = page;
        ViewBag.PageSize = Pagination.DefaultPageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)Pagination.DefaultPageSize);
        ViewBag.ControllerName = "SyndicationFeedItems";
        ViewBag.ActionName = "Index";
        ViewBag.SortBy = sortBy;
        ViewBag.SortDescending = sortDescending;
        ViewBag.Filter = filter;

        return View(viewSources);
    }

    /// <summary>
    /// Returns a detail view of the syndication feed source
    /// </summary>
    /// <param name="id">The id of the syndication feed source</param>
    /// <returns>A <see cref="SyndicationFeedItemViewModel"/></returns>
    public async Task<IActionResult> Details(int id)
    {
        var source = await _syndicationFeedItemService.GetAsync(id);
        if (source == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this syndication feed source.";
                return RedirectToAction("Index");
            }
        }

        var sourceViewModel = _mapper.Map<SyndicationFeedItemViewModel>(source);
        return View(sourceViewModel);
    }

    /// <summary>
    /// Adds a new syndication feed source
    /// </summary>
    /// <returns>The add new syndication feed source view</returns>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public IActionResult Add()
    {
        return View(new SyndicationFeedItemViewModel { PublicationDate = DateTimeOffset.UtcNow });
    }

    /// <summary>
    /// Adds a new syndication feed source
    /// </summary>
    /// <param name="itemViewModel">The <see cref="SyndicationFeedItemViewModel"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redirects to the <see cref="Add()"/> page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Add(SyndicationFeedItemViewModel itemViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(itemViewModel);
        }

        var sourceToAdd = _mapper.Map<SyndicationFeedItem>(itemViewModel);
        sourceToAdd.CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty;
        
        var savedSource = await _syndicationFeedItemService.SaveAsync(sourceToAdd);
        if (savedSource == null)
        {
            TempData["ErrorMessage"] = "Failed to add the syndication feed source.";
            return RedirectToAction("Add");
        }
        
        TempData["SuccessMessage"] = "Syndication feed source added successfully.";
        return RedirectToAction("Details", new { id = savedSource.Id });
    }

    /// <summary>
    /// Shows the delete confirmation page for a syndication feed source
    /// </summary>
    /// <param name="id">The identity of a syndication feed source</param>
    /// <returns>The delete confirmation view</returns>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var source = await _syndicationFeedItemService.GetAsync(id);
        if (source == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this syndication feed source.";
                return RedirectToAction("Index");
            }
        }

        var sourceViewModel = _mapper.Map<SyndicationFeedItemViewModel>(source);
        return View(sourceViewModel);
    }

    /// <summary>
    /// Deletes a syndication feed source after confirmation
    /// </summary>
    /// <param name="id">The identity of a syndication feed source</param>
    /// <returns>Upon success, redirects to the <see cref="Index"/>. Upon failure, reloads the view.</returns>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var source = await _syndicationFeedItemService.GetAsync(id);
        if (source == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this syndication feed source.";
                return RedirectToAction("Index");
            }
        }

        var result = await _syndicationFeedItemService.DeleteAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Syndication feed source deleted successfully.";
            return RedirectToAction("Index");
        }

        var sourceViewModel = _mapper.Map<SyndicationFeedItemViewModel>(source);
        ModelState.AddModelError(string.Empty, "Failed to delete the syndication feed source.");
        return View(sourceViewModel);
    }
}
