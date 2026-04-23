using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for user-facing help pages.
/// </summary>
[Authorize]
public class HelpController(ISocialMediaPlatformService socialMediaPlatformService) : Controller
{
    [Route("Help/SocialMediaPlatforms/{platform}")]
    public async Task<IActionResult> SocialMediaPlatforms(string platform)
    {
        var platforms = await socialMediaPlatformService.GetAllAsync();
        var match = platforms.FirstOrDefault(p =>
            string.Equals(p.Name, platform, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return NotFound();
        return View($"SocialMediaPlatforms/{match.Name}");
    }
}
