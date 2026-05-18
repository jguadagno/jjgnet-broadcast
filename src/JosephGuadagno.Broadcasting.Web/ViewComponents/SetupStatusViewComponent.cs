using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.ViewComponents;

/// <summary>
/// Renders a navigation badge that prompts the user to complete their account setup.
/// Visible for authenticated users until all three setup areas are configured.
/// </summary>
public class SetupStatusViewComponent(ISetupService setupService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var status = await setupService.GetSetupStatusAsync();
        return View(status);
    }
}
