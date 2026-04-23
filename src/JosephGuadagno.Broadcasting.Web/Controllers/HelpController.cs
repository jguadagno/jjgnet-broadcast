using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for user-facing help pages.
/// </summary>
[Authorize]
public class HelpController : Controller
{
    private static readonly Dictionary<string, string> PlatformViewMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bluesky"]  = "SocialMediaPlatforms/Bluesky",
        ["twitter"]  = "SocialMediaPlatforms/Twitter",
        ["linkedin"] = "SocialMediaPlatforms/LinkedIn",
        ["facebook"] = "SocialMediaPlatforms/Facebook",
        ["mastodon"] = "SocialMediaPlatforms/Mastodon"
    };

    [Route("Help/SocialMediaPlatforms/{platform}")]
    public IActionResult SocialMediaPlatforms(string platform)
    {
        if (!PlatformViewMap.TryGetValue(platform, out var viewName))
            return NotFound();
        return View(viewName);
    }
}
