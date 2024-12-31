using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostImage
{
    private readonly ILinkedInManager _linkedInManager;
    private readonly HttpClient _httpClient;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<PostImage> _logger;
    
    public PostImage(ILinkedInManager linkedInManager, HttpClient httpClient, TelemetryClient telemetryClient, ILogger<PostImage> logger)
    {
        _linkedInManager = linkedInManager;
        _httpClient = httpClient;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    [Function("linkedin_post_image")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.LinkedInPostImage)]
        LinkedInPostImage linkedInPostImage)
    {
        try
        {
            var imageResponse = await _httpClient.GetAsync(linkedInPostImage.ImageUrl);
            if (imageResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Unable to get the image from the url: {ImageUrl}. Status Code: {StatusCode}",
                    linkedInPostImage.ImageUrl, imageResponse.StatusCode);
                return;
            }
            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
            
            var linkedInShareId = await _linkedInManager.PostShareTextAndImage(linkedInPostImage.AccessToken, linkedInPostImage.AuthorId,
                linkedInPostImage.Text, imageBytes, linkedInPostImage.Title,
                linkedInPostImage.Description);

            if (!string.IsNullOrEmpty(linkedInShareId))
            {
                _telemetryClient.TrackEvent(Constants.Metrics.LinkedInPostImage, new Dictionary<string, string>
                {
                    {"linkedInShareId", linkedInShareId},
                    {"imageUrl", linkedInPostImage.ImageUrl},
                    {"title", linkedInPostImage.Title}, 
                    {"url", linkedInPostImage.ImageUrl}
                });
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to post the image to LinkedIn. Exception: {ExceptionMessage}",
                exception.Message);
        }
    }
}