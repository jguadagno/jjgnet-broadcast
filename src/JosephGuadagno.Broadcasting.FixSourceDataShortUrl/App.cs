using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.FixSourceDataShortUrl.Models;
using JosephGuadagno.Utilities.Web.Shortener;

using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.FixSourceDataShortUrl;

public class App(
    IYouTubeItemManager YouTubeItemManager,
    ISyndicationFeedItemManager SyndicationFeedItemManager,
    Bitly bitly,
    Settings settings,
    ILogger<App> logger)
{
    public async Task Run()
    {
        logger.LogInformation("Getting items from the SyndicationFeed ");
        var syndicationItems = await SyndicationFeedItemManager.GetAllAsync();
        if (!syndicationItems.Any())
        {
            logger.LogInformation("There were no syndication items found");
        }

        foreach (var sourceData in syndicationItems)
        {
            if (sourceData.ShortenedUrl is not null &&  sourceData.ShortenedUrl.Contains(settings.BitlyShortenedDomain))
            {
                // we can skip this record
                continue;
            }

            try
            {
                var shortenedUrl = await bitly.Shorten(sourceData.Url, settings.BitlyShortenedDomain);
                if (shortenedUrl is null || string.IsNullOrEmpty(shortenedUrl.Link))
                {
                    logger.LogInformation("Could not update Syndication Item Url for '{SourceDataUrl}'", sourceData.Url);
                    continue;
                }

                sourceData.ShortenedUrl = shortenedUrl.Link;
                sourceData.LastUpdatedOn = DateTime.UtcNow;
                await SyndicationFeedItemManager.SaveAsync(sourceData);
                logger.LogInformation("Updated Syndication Feed item for '{SourceUrl}'", sourceData.Url);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating syndication feed item. Source Url: {SourceUrl}", sourceData.Url);
            }
        }

        logger.LogInformation("Getting items from the YouTube table");
        var youtubeItems = await YouTubeItemManager.GetAllAsync();
        foreach (var sourceData in youtubeItems)
        {
            if (sourceData.ShortenedUrl is not null &&  sourceData.ShortenedUrl.Contains(settings.BitlyShortenedDomain))
            {
                // we can skip this record
                continue;
            }

            try
            {
                var shortenedUrl = await bitly.Shorten(sourceData.Url, settings.BitlyShortenedDomain);
                if (shortenedUrl is null || string.IsNullOrEmpty(shortenedUrl.Link))
                {
                    logger.LogInformation("Could not update YouTube Url for '{SourceDataUrl}'", sourceData.Url);
                    continue;
                }

                sourceData.ShortenedUrl = shortenedUrl.Link;
                sourceData.LastUpdatedOn = DateTime.UtcNow;
                await YouTubeItemManager.SaveAsync(sourceData);
                logger.LogInformation("Updated YouTube Item for '{SourceUrl}'", sourceData.Url);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating YouTube feed item. Source Url: {SourceUrl}", sourceData.Url);
            }
        }
    }
}