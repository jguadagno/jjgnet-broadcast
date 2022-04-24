using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SchedulesController
{
    private readonly IScheduledItemManager _scheduledItemManager;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(IScheduledItemManager scheduledItemManager, ILogger<SchedulesController> logger)
    {
        _scheduledItemManager = scheduledItemManager;
        _logger = logger;
    }
}