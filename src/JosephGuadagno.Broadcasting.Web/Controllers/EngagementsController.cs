using AutoMapper;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// This is the controller for managing the engagements.
/// </summary>
[Authorize(Policy = "RequireViewer")]
public class EngagementsController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly ISocialMediaPlatformService _socialMediaPlatformService;
    private readonly IMapper _mapper;

    /// <summary>
    /// The constructor for the EngagementsController.
    /// </summary>
    /// <param name="engagementService">The engagement service</param>
    /// <param name="socialMediaPlatformService">The social media platform service</param>
    /// <param name="mapper">The mapper service</param>
    public EngagementsController(IEngagementService engagementService, ISocialMediaPlatformService socialMediaPlatformService, IMapper mapper)
    {
        _engagementService = engagementService;
        _socialMediaPlatformService = socialMediaPlatformService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the list of engagements. 
    /// </summary>
    /// <returns>Returns a List&lt;<see cref="EngagementViewModel"/>&gt;.</returns>
    public async Task<IActionResult> Index(int page = Pagination.DefaultPage)
    {
        var result = await _engagementService.GetEngagementsAsync(page, Pagination.DefaultPageSize);
        var viewEngagements = _mapper.Map<List<EngagementViewModel>>(result.Items);

        ViewBag.Page = page;
        ViewBag.PageSize = Pagination.DefaultPageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)Pagination.DefaultPageSize);
        ViewBag.ControllerName = "Engagements";
        ViewBag.ActionName = "Index";

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

        // Load platforms for this engagement
        var platforms = await _engagementService.GetPlatformsForEngagementAsync(id);
        engagementViewModel.SocialMediaPlatforms = _mapper.Map<List<EngagementSocialMediaPlatformViewModel>>(platforms);

        return View(engagementViewModel);
    }

    /// <summary>
    /// Edits an engagement.
    /// </summary>
    /// <param name="id">The id of the engagement</param>
    /// <returns>An <see cref="EngagementViewModel"/></returns>
    [Authorize(Policy = "RequireContributor")]
    public async Task<IActionResult> Edit(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null)
        {
            return NotFound();
        }

        var engagementViewModel = _mapper.Map<EngagementViewModel>(engagement);

        // Load platforms for this engagement
        var platforms = await _engagementService.GetPlatformsForEngagementAsync(id);
        engagementViewModel.SocialMediaPlatforms = _mapper.Map<List<EngagementSocialMediaPlatformViewModel>>(platforms);

        return View(engagementViewModel);
    }

    /// <summary>
    /// Applies the changes to the engagement.
    /// </summary>
    /// <param name="engagementViewModel">The <see cref="EngagementViewModel"/> to edit.</param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redirects to the <see cref="Edit(int)"/> page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireContributor")]
    public async Task<IActionResult> Edit(EngagementViewModel engagementViewModel)
    {
        var engagementToEdit = _mapper.Map<Domain.Models.Engagement>(engagementViewModel);
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagementToEdit);
        if (savedEngagement == null)
        {
            TempData["ErrorMessage"] = "Failed to update the engagement.";
            return RedirectToAction("Edit", new { id = engagementViewModel.Id });
        }
        TempData["SuccessMessage"] = "Engagement updated successfully.";
        return RedirectToAction("Details", new { id = savedEngagement.Id });
    }

    /// <summary>
    /// Shows the delete confirmation page for an engagement.
    /// </summary>
    /// <param name="id">The identity of an engagement</param>
    /// <returns>The delete confirmation view.</returns>
    [HttpGet]
    [Authorize(Policy = "RequireContributor")]
    public async Task<IActionResult> Delete(int id)
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
    /// Deletes an engagement after confirmation.
    /// </summary>
    /// <param name="id">The identity of an engagement</param>
    /// <returns>Upon success, redirects to the <see cref="Index"/>. Upon failure, reloads the view.</returns>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireContributor")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var engagement = await _engagementService.GetEngagementAsync(id);
        if (engagement == null) return NotFound();

        if (!User.IsInRole(RoleNames.Administrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || engagement.CreatedByEntraOid == null || engagement.CreatedByEntraOid != currentUserOid)
                return Forbid();
        }

        var result = await _engagementService.DeleteEngagementAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Engagement deleted successfully.";
            return RedirectToAction("Index");
        }

        var engagementViewModel = _mapper.Map<EngagementViewModel>(engagement);
        ModelState.AddModelError(string.Empty, "Failed to delete the engagement.");
        return View(engagementViewModel);
    }

    /// <summary>
    /// Returns all engagements as FullCalendar-compatible JSON events.
    /// </summary>
    /// <returns>A JSON array of calendar events.</returns>
    [HttpGet]
    public async Task<JsonResult> GetCalendarEvents()
    {
        var result = await _engagementService.GetEngagementsAsync();
        if (result.Items.Count == 0)
        {
            return Json(Array.Empty<object>());
        }

        var events = result.Items.Select(e => new
        {
            id = e.Id.ToString(),
            title = e.Name,
            start = e.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = e.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            url = e.Url
        });

        return Json(events);
    }

    /// <summary>
    /// Adds a new engagement.
    /// </summary>
    /// <returns>The add new engagement view.</returns>
    [Authorize(Policy = "RequireContributor")]
    public IActionResult Add()
    {
        return View(new EngagementViewModel
            { StartDateTime = DateTime.UtcNow, EndDateTime = DateTime.UtcNow.AddHours(1) });
    }

    /// <summary>
    /// Adds a new engagement.
    /// </summary>
    /// <param name="engagementViewModel">The <see cref="EngagementViewModel"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, redirects to the <see cref="Edit(int)"/> page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireContributor")]
    public async Task<RedirectToActionResult> Add(EngagementViewModel engagementViewModel)
    {
        var engagementToAdd = _mapper.Map<Domain.Models.Engagement>(engagementViewModel);
        engagementToAdd.CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        var savedEngagement = await _engagementService.SaveEngagementAsync(engagementToAdd);
        if (savedEngagement == null)
        {
            TempData["ErrorMessage"] = "Failed to add the engagement.";
            return RedirectToAction("Add");
        }
        TempData["SuccessMessage"] = "Engagement added successfully.";
        return RedirectToAction("Details", new { id = savedEngagement.Id });
    }

    /// <summary>
    /// Shows the form to add a social media platform to an engagement.
    /// </summary>
    /// <param name="engagementId">The identity of the engagement</param>
    /// <returns>The add platform view.</returns>
    [HttpGet]
    [Authorize(Policy = "RequireContributor")]
    public async Task<IActionResult> AddPlatform(int engagementId)
    {
        var allPlatforms = await _socialMediaPlatformService.GetAllAsync(includeInactive: true);
        ViewBag.Platforms = allPlatforms;
        return View(new EngagementSocialMediaPlatformViewModel { EngagementId = engagementId });
    }

    /// <summary>
    /// Adds a social media platform to an engagement.
    /// </summary>
    /// <param name="engagementId">The identity of the engagement</param>
    /// <param name="vm">The platform view model</param>
    /// <returns>Upon success, redirects to the Edit page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireContributor")]
    public async Task<IActionResult> AddPlatform(int engagementId, EngagementSocialMediaPlatformViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var allPlatforms = await _socialMediaPlatformService.GetAllAsync();
            ViewBag.Platforms = allPlatforms;
            return View(vm);
        }

        var result = await _engagementService.AddPlatformToEngagementAsync(engagementId, vm.SocialMediaPlatformId, vm.Handle);
        if (result is null)
        {
            TempData["ErrorMessage"] = "Failed to add platform to engagement.";
        }
        else
        {
            TempData["SuccessMessage"] = "Platform added successfully.";
        }

        return RedirectToAction("Edit", new { id = engagementId });
    }

    /// <summary>
    /// Removes a social media platform from an engagement.
    /// </summary>
    /// <param name="engagementId">The identity of the engagement</param>
    /// <param name="platformId">The identity of the platform to remove</param>
    /// <returns>Redirects to the Edit page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireContributor")]
    public async Task<IActionResult> RemovePlatform(int engagementId, int platformId)
    {
        var result = await _engagementService.RemovePlatformFromEngagementAsync(engagementId, platformId);
        if (!result)
        {
            TempData["ErrorMessage"] = "Failed to remove platform from engagement.";
        }
        else
        {
            TempData["SuccessMessage"] = "Platform removed successfully.";
        }

        return RedirectToAction("Edit", new { id = engagementId });
    }
}