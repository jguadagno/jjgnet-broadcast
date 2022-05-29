using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

public class TalksController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly ILogger<TalksController> _logger;

    public TalksController(IEngagementService engagementService, ILogger<TalksController> logger)
    {
        _engagementService = engagementService;
        _logger = logger;
    }
    
    [Route("engagements/{engagementId:int}/[controller]/[action]/{talkId:int}")]
    public async Task<IActionResult> Details(int engagementId, int talkId)
    {
        var talk = await _engagementService.GetEngagementTalkAsync(engagementId, talkId);
        if (talk == null)
        {
            return NotFound();
        }
        return View(talk);
    }
        
    [Route("engagements/{engagementId:int}/[controller]/[action]/{talkId:int}")]
    public async Task<IActionResult> Edit(int engagementId, int talkId)
    {
        var talk = await _engagementService.GetEngagementTalkAsync(engagementId, talkId);
        if (talk == null)
        {
            return NotFound();
        }
        return View(talk);
    }

    [HttpPost]
    [Route("engagements/{engagementId:int}/[controller]/[action]/{talkId:int}")]
    public async Task<IActionResult> Edit(Talk talk)
    {
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talk);
        return savedTalk == null
            ? RedirectToAction("Edit", new { engagementId = talk.EngagementId, talkId = talk.Id })
            : RedirectToAction("Details", new { engagementId = savedTalk.EngagementId, talkId = savedTalk.Id });
    }

    [HttpGet]
    [Route("engagements/{engagementId:int}/[controller]/[action]/{talkId:int}")]
    public async Task<IActionResult> Delete(int engagementId, int talkId)
    {
        var result = await _engagementService.DeleteEngagementTalkAsync(engagementId, talkId);

        if (result)
        {
            return RedirectToAction("Edit", "Engagements", new {id = engagementId});
        }
        return View();
    }
        
    [Route("engagements/{engagementId:int}/[controller]/[action]")]
    public IActionResult Add(int engagementId)
    {
        return View(new Talk{EngagementId = engagementId});
    }

    [HttpPost]
    [Route("engagements/{engagementId:int}/[controller]/[action]")]
    public async Task<RedirectToActionResult> Add(Talk talk)
    {
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talk);
        return savedTalk == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { engagementId = savedTalk.EngagementId, talkId = savedTalk.Id });
    }
}