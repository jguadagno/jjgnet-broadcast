using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>Shows the aggregate dispatcher settings summary for the current user.</summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
[Route("Dispatchers")]
public class DispatchersController(
    IDispatchersAggregateService aggregateService,
    ISocialMediaPlatformService platformService) : Controller
{
    private static readonly IReadOnlyDictionary<string, string> PlatformControllerMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Twitter"] = "PlatformTwitterSettings",
            ["Bluesky"] = "PlatformBlueskySettings",
            ["LinkedIn"] = "PlatformLinkedInSettings",
            ["Facebook"] = "PlatformFacebookSettings",
        };

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var aggregateTask = aggregateService.GetCurrentUserAsync();
        var platformsTask = platformService.GetAllAsync(pageSize: 50);

        var aggregate = await aggregateTask;
        var platformsResult = await platformsTask;

        var cards = platformsResult.Items
            .Where(p => p.IsActive && PlatformControllerMap.ContainsKey(p.Name))
            .Select(p => new DispatcherPlatformCardViewModel
            {
                Name = p.Name,
                Icon = p.Icon ?? string.Empty,
                Controller = PlatformControllerMap[p.Name],
                IsConfigured = IsConfigured(aggregate, p.Name),
                IsEnabled = IsEnabled(aggregate, p.Name),
            })
            .ToList();

        var viewModel = aggregate ?? new DispatchersAggregateViewModel();
        viewModel.Platforms = cards;

        return View(viewModel);
    }

    private static bool IsConfigured(DispatchersAggregateViewModel? aggregate, string platformName) =>
        platformName switch
        {
            "Twitter" => aggregate?.Twitter is not null,
            "Bluesky" => aggregate?.Bluesky is not null,
            "LinkedIn" => aggregate?.LinkedIn is not null,
            "Facebook" => aggregate?.Facebook is not null,
            _ => false,
        };

    private static bool IsEnabled(DispatchersAggregateViewModel? aggregate, string platformName) =>
        platformName switch
        {
            "Twitter" => aggregate?.Twitter?.IsEnabled ?? false,
            "Bluesky" => aggregate?.Bluesky?.IsEnabled ?? false,
            "LinkedIn" => aggregate?.LinkedIn?.IsEnabled ?? false,
            "Facebook" => aggregate?.Facebook?.IsEnabled ?? false,
            _ => false,
        };
}
