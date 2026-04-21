using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for the talks.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
[Route("engagements/{engagementId:int}/[controller]/[action]")]
public class TalksController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly IMapper _mapper;

    /// <summary>
    /// The constructor for the talk's controller.
    /// </summary>
    /// <param name="engagementService">The engagement service</param>
    /// <param name="mapper">The mapper service</param>
    public TalksController(IEngagementService engagementService, IMapper mapper)
    {
        _engagementService = engagementService;
        _mapper = mapper;
    }
    
    /// <summary>
    /// The details view for a talk.
    /// </summary>
    /// <param name="engagementId">The id of the engagement</param>
    /// <param name="talkId">The id of the talk</param>
    /// <returns>A view with the details of the talk</returns>
    [Route("{talkId:int}")]
    public async Task<IActionResult> Details(int engagementId, int talkId)
    {
        var talk = await _engagementService.GetEngagementTalkAsync(engagementId, talkId);
        if (talk == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || talk.CreatedByEntraOid == null || talk.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this talk.";
                return RedirectToAction("Edit", "Engagements", new { id = engagementId });
            }
        }
        
        var talkViewModel = _mapper.Map<TalkViewModel>(talk);
        return View(talkViewModel);
    }
    
    /// <summary>
    /// Edits the details of a talk.
    /// </summary>
    /// <param name="engagementId">The id of the engagement</param>
    /// <param name="talkId">The id of the talk to edit</param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> view.</returns>
    [Route("{talkId:int}")]
    public async Task<IActionResult> Edit(int engagementId, int talkId)
    {
        var talk = await _engagementService.GetEngagementTalkAsync(engagementId, talkId);
        if (talk == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || talk.CreatedByEntraOid == null || talk.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this talk.";
                return RedirectToAction("Edit", "Engagements", new { id = engagementId });
            }
        }

        var talkViewModel = _mapper.Map<TalkViewModel>(talk);
        return View(talkViewModel);
    }

    /// <summary>
    /// Edits the talk
    /// </summary>
    /// <param name="talkViewModel">The <see cref="TalkViewModel"/> to edit</param>
    /// <returns>Upon success, redirected to the <see cref="Details"/>. Upon failure, the view will be reloaded</returns>
    [HttpPost]
    [Route("{talkId:int}")]
    public async Task<IActionResult> Edit(TalkViewModel talkViewModel)
    {
        // Defence-in-depth: re-verify ownership before saving (issue #742)
        if (talkViewModel.EngagementId == null)
        {
            return NotFound();
        }
        var existingTalk = await _engagementService.GetEngagementTalkAsync(talkViewModel.EngagementId.Value, talkViewModel.Id);
        if (existingTalk == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || existingTalk.CreatedByEntraOid == null || existingTalk.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this talk.";
                return RedirectToAction("Edit", "Engagements", new { id = talkViewModel.EngagementId });
            }
        }

        var talkToEdit = _mapper.Map<Domain.Models.Talk>(talkViewModel);
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talkToEdit);
        if (savedTalk == null)
        {
            TempData["ErrorMessage"] = "Failed to update the talk.";
            return RedirectToAction("Edit", new { engagementId = talkViewModel.EngagementId, talkId = talkViewModel.Id });
        }
        TempData["SuccessMessage"] = "Talk updated successfully.";
        return RedirectToAction("Details", new { engagementId = savedTalk.EngagementId, talkId = savedTalk.Id });
    }

    /// <summary>
    /// Shows the delete confirmation page for a talk.
    /// </summary>
    /// <param name="engagementId">The id of the engagement</param>
    /// <param name="talkId">The id of the talk to delete</param>
    /// <returns>The delete confirmation view.</returns>
    [HttpGet]
    [Route("{talkId:int}")]
    public async Task<IActionResult> Delete(int engagementId, int talkId)
    {
        var talk = await _engagementService.GetEngagementTalkAsync(engagementId, talkId);
        if (talk == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || talk.CreatedByEntraOid == null || talk.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this talk.";
                return RedirectToAction("Edit", "Engagements", new { id = engagementId });
            }
        }

        var talkViewModel = _mapper.Map<TalkViewModel>(talk);
        return View(talkViewModel);
    }

    /// <summary>
    /// Deletes a talk after confirmation.
    /// </summary>
    /// <param name="engagementId">The id of the engagement</param>
    /// <param name="talkId">The id of the talk to delete</param>
    /// <returns>Upon success, redirects to the <see cref="Edit(int,int)"/> view.</returns>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Route("{talkId:int}")]
    public async Task<IActionResult> DeleteConfirmed(int engagementId, int talkId)
    {
        var talk = await _engagementService.GetEngagementTalkAsync(engagementId, talkId);
        if (talk == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || talk.CreatedByEntraOid == null || talk.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this talk.";
                return RedirectToAction("Edit", "Engagements", new { id = engagementId });
            }
        }

        var result = await _engagementService.DeleteEngagementTalkAsync(engagementId, talkId);

        if (result)
        {
            TempData["SuccessMessage"] = "Talk deleted successfully.";
            return RedirectToAction("Edit", "Engagements", new { id = engagementId });
        }

        TempData["ErrorMessage"] = "Failed to delete the talk.";
        var talkViewModel = _mapper.Map<TalkViewModel>(talk);
        ModelState.AddModelError(string.Empty, "Failed to delete the talk.");
        return View(talkViewModel);
    }
    
    /// <summary>
    /// Added a new talk.
    /// </summary>
    /// <param name="engagementId">The id of the engagement to add</param>
    /// <returns>A view to add a talk</returns>
    [Route("")]
    public IActionResult Add(int engagementId)
    {
        return View(new TalkViewModel
        {
            EngagementId = engagementId, StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        });
    }

    /// <summary>
    /// Adds a new talk.
    /// </summary>
    /// <param name="talkViewModel">The <see cref="TalkViewModel"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, reloads the page.</returns>
    [HttpPost]
    [Route("")]
    public async Task<RedirectToActionResult> Add(TalkViewModel talkViewModel)
    {
        var talkToAdd = _mapper.Map<Domain.Models.Talk>(talkViewModel);
        talkToAdd.CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talkToAdd);
        if (savedTalk == null)
        {
            TempData["ErrorMessage"] = "Failed to add the talk.";
            return RedirectToAction("Add");
        }
        TempData["SuccessMessage"] = "Talk added successfully.";
        return RedirectToAction("Details", new { engagementId = savedTalk.EngagementId, talkId = savedTalk.Id });
    }
}
