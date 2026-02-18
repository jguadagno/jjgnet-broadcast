using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostImage(
    ILinkedInManager linkedInManager,
    HttpClient httpClient,
    TelemetryClient telemetryClient,
    ILogger<PostImage> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInPostImage)]
    public async Task Run(
        [QueueTrigger(Queues.LinkedInPostImage)]
        LinkedInPostImage linkedInPostImage)
    {
        try
        {
            var imageResponse = await httpClient.GetAsync(linkedInPostImage.ImageUrl);
            if (imageResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Unable to get the image from the url: {ImageUrl}. Status Code: {StatusCode}",
                    linkedInPostImage.ImageUrl, imageResponse.StatusCode);
                return;
            }
            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
            
            var linkedInShareId = await linkedInManager.PostShareTextAndImage(linkedInPostImage.AccessToken, linkedInPostImage.AuthorId,
                linkedInPostImage.Text, imageBytes, linkedInPostImage.Title,
                linkedInPostImage.Description);

            if (!string.IsNullOrEmpty(linkedInShareId))
            {
                telemetryClient.TrackEvent(Metrics.LinkedInPostImage, new Dictionary<string, string>
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
            logger.LogError(exception, "Unable to post the image to LinkedIn. Exception: {ExceptionMessage}",
                exception.Message);
        }
    }
}