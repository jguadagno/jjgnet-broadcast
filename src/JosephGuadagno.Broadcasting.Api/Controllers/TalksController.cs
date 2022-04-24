using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TalksController
{
    private readonly IEngagementManager _engagementManager;
    private readonly ILogger<TalksController> _logger;

    public TalksController(IEngagementManager engagementManager, ILogger<TalksController> logger)
    {
        _engagementManager = engagementManager;
        _logger = logger;
    }
}