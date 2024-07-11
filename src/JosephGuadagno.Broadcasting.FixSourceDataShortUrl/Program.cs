using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.FixSourceDataShortUrl.Models;
using JosephGuadagno.Utilities.Web.Shortener;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Starting Application");
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true)
    .AddUserSecrets<Program>()
    .Build();

var settings = new Settings();
config.Bind("Settings", settings);

var bitly = new Bitly(new HttpClient(), new BitlyConfiguration()
{
    ApiRootUri = settings.BitlyApiRootUri,
    Token = settings.BitlyToken
});

var sourceDataRepository = new SourceDataRepository(settings.StorageAccount);

Console.WriteLine("Getting items from the SyndicationFeed table");
var syndicationItems = await sourceDataRepository.GetAllAsync("SyndicationFeed");
if (!syndicationItems.Any())
{
    Console.WriteLine("There were no syndication items found");
}

foreach (var sourceData in syndicationItems)
{
    if (sourceData.ShortenedUrl.Contains("jjg.me"))
    {
        // we can skip this record
        continue;
    }

    var shortenedUrl = await bitly.Shorten(sourceData.Url, settings.BitlyShortenedDomain);
    if (shortenedUrl is null || string.IsNullOrEmpty(shortenedUrl.Link))
    {
        Console.WriteLine($"Could not update url: {sourceData.Url}");
        continue;
    }

    sourceData.ShortenedUrl = shortenedUrl.Link;
    sourceData.UpdatedOnDate = DateTime.UtcNow;
    var updated = await sourceDataRepository.SaveAsync(sourceData);
    Console.WriteLine(updated ? $"Updated {sourceData.Url}" : $"Failed to update {sourceData.Url}");
}

Console.WriteLine("Getting items from the YouTube table");
var youtubeItems = await sourceDataRepository.GetAllAsync("YouTube");
foreach (var sourceData in youtubeItems)
{
    if (sourceData.ShortenedUrl.Contains("jjg.me"))
    {
        // we can skip this record
        continue;
    }

    var shortenedUrl = await bitly.Shorten(sourceData.Url, settings.BitlyShortenedDomain);
    if (shortenedUrl is null || string.IsNullOrEmpty(shortenedUrl.Link))
    {
        Console.WriteLine($"Could not update url: {sourceData.Url}");
        continue;
    }

    sourceData.ShortenedUrl = shortenedUrl.Link;
    sourceData.UpdatedOnDate = DateTime.UtcNow;
    var updated = await sourceDataRepository.SaveAsync(sourceData);
    Console.WriteLine(updated ? $"Updated {sourceData.Url}" : $"Failed to update {sourceData.Url}");
}

Console.WriteLine("Done.");
