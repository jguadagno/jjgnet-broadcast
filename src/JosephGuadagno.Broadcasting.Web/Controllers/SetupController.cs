using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for the post-approval user onboarding setup checklist.
/// Requires at least the Contributor role so the page is not exposed to unapproved users.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
public class SetupController(
    ISetupService setupService,
    ILogger<SetupController> logger) : Controller
{
    /// <summary>
    /// Renders the onboarding setup checklist for the current user.
    /// Always fetches fresh data so the page reflects the latest configuration state.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        logger.LogDebug("Setup checklist requested by {User}", User.Identity?.Name);
        var status = await setupService.GetSetupStatusAsync(forceRefresh: true);
        return View(status);
    }
}
