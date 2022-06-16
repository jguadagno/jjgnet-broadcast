using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for the talks.
/// </summary>
[Route("engagements/{engagementId:int}/[controller]/[action]")]
public class TalksController : Controller
{
    private readonly IEngagementService _engagementService;
    private readonly ILogger<TalksController> _logger;

    /// <summary>
    /// The constructor for the talks controller.
    /// </summary>
    /// <param name="engagementService">The engagement service</param>
    /// <param name="logger">The logger to use</param>
    public TalksController(IEngagementService engagementService, ILogger<TalksController> logger)
    {
        _engagementService = engagementService;
        _logger = logger;
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
        return View(talk);
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
        return View(talk);
    }

    /// <summary>
    /// Edits the talk
    /// </summary>
    /// <param name="talk">The <see cref="Talk"/> to edit</param>
    /// <returns>Upon success, redirected to the <see cref="Details"/>. Upon failure, the view will be reloaded</returns>
    [HttpPost]
    [Route("{talkId:int}")]
    public async Task<IActionResult> Edit(Talk talk)
    {
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talk);
        return savedTalk == null
            ? RedirectToAction("Edit", new { engagementId = talk.EngagementId, talkId = talk.Id })
            : RedirectToAction("Details", new { engagementId = savedTalk.EngagementId, talkId = savedTalk.Id });
    }

    /// <summary>
    /// Deletes a talk.
    /// </summary>
    /// <param name="engagementId">The id of the engagement</param>
    /// <param name="talkId">The id of the talk to delete</param>
    /// <returns>Upon success, redirects to the <see cref="Edit(int,int)"/> view.</returns>
    [HttpGet]
    [Route("{talkId:int}")]
    public async Task<IActionResult> Delete(int engagementId, int talkId)
    {
        var result = await _engagementService.DeleteEngagementTalkAsync(engagementId, talkId);

        if (result)
        {
            return RedirectToAction("Edit", "Engagements", new {id = engagementId});
        }
        return View();
    }
    
    /// <summary>
    /// Added a new talk.
    /// </summary>
    /// <param name="engagementId">The id of the engagement to add</param>
    /// <returns>A view to add a talk</returns>
    [Route("")]
    public IActionResult Add(int engagementId)
    {
        return View(new Talk{EngagementId = engagementId});
    }

    /// <summary>
    /// Adds a new talk.
    /// </summary>
    /// <param name="talk">The <see cref="Talk"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, reloads the page.</returns>
    [HttpPost]
    [Route("")]
    public async Task<RedirectToActionResult> Add(Talk talk)
    {
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talk);
        return savedTalk == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { engagementId = savedTalk.EngagementId, talkId = savedTalk.Id });
    }
}