using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EngagementsController
{
    private readonly IEngagementManager _engagementManager;
    private readonly ILogger<EngagementsController> _logger;

    public EngagementsController(IEngagementManager engagementManager, ILogger<EngagementsController> logger)
    {
        _engagementManager = engagementManager;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<List<Engagement>> GetEngagements()
    {
        _logger.LogDebug("Call to Engagements.GetEngagements");

        return await _engagementManager.GetAllAsync();
    }

}