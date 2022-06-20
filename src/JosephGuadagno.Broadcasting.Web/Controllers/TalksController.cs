using AutoMapper;
using JosephGuadagno.Broadcasting.Web.Models;
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
    private readonly IMapper _mapper;
    private readonly ILogger<TalksController> _logger;

    /// <summary>
    /// The constructor for the talks controller.
    /// </summary>
    /// <param name="engagementService">The engagement service</param>
    /// <param name="mapper">The mapper service</param>
    /// <param name="logger">The logger to use</param>
    public TalksController(IEngagementService engagementService, IMapper mapper, ILogger<TalksController> logger)
    {
        _engagementService = engagementService;
        _mapper = mapper;
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
        
        var talkViewModel = _mapper.Map<TalkViewModel>(talk);
        return View(talkViewModel);
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
        var talkViewModel = _mapper.Map<TalkViewModel>(talk);
        return View(talkViewModel);
    }

    /// <summary>
    /// Edits the talk
    /// </summary>
    /// <param name="talkViewModel">The <see cref="TalkViewModel"/> to edit</param>
    /// <returns>Upon success, redirected to the <see cref="Details"/>. Upon failure, the view will be reloaded</returns>
    [HttpPost]
    [Route("{talkId:int}")]
    public async Task<IActionResult> Edit(TalkViewModel talkViewModel)
    {
        var talkToEdit = _mapper.Map<Domain.Models.Talk>(talkViewModel);
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talkToEdit);
        return savedTalk == null
            ? RedirectToAction("Edit", new { engagementId = talkViewModel.EngagementId, talkId = talkViewModel.Id })
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
        return View(new TalkViewModel{EngagementId = engagementId});
    }

    /// <summary>
    /// Adds a new talk.
    /// </summary>
    /// <param name="talkViewModel">The <see cref="TalkViewModel"/></param>
    /// <returns>Upon success, redirects to the <see cref="Details"/> page. Upon failure, reloads the page.</returns>
    [HttpPost]
    [Route("")]
    public async Task<RedirectToActionResult> Add(TalkViewModel talkViewModel)
    {
        var talkToAdd = _mapper.Map<Domain.Models.Talk>(talkViewModel);
        var savedTalk = await _engagementService.SaveEngagementTalkAsync(talkToAdd);
        return savedTalk == null
            ? RedirectToAction("Add")
            : RedirectToAction("Details", new { engagementId = savedTalk.EngagementId, talkId = savedTalk.Id });
    }
}