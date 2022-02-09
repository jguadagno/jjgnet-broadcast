using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class ProcessNewSourceData
{
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly ILogger<ProcessNewSourceData> _logger;

    public ProcessNewSourceData(SourceDataRepository sourceDataRepository, ILogger<ProcessNewSourceData> logger)
    {
        _sourceDataRepository = sourceDataRepository;
        _logger = logger;
    }
        
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_new_source_data`
    [FunctionName("facebook_process_new_source_data")]
    public async Task RunAsync(
        [EventGridTrigger()] EventGridEvent eventGridEvent,
        [Queue(Constants.Queues.FacebookPostStatusToPage)] 
        ICollector<FacebookPostStatus> outboundMessages)
    {
        // Get the Source Data identifier for the event
        if (eventGridEvent.Data is null)
        {
            _logger.LogError("The event data was null for event '{eventGridEvent.Id}'", eventGridEvent.Id);
            return;
        }
        var tableEvent = JsonSerializer.Deserialize<TableEvent>(eventGridEvent.Data.ToString());
        if (tableEvent == null)
        {
            _logger.LogError("Failed to parse the TableEvent data for event '{eventGridEvent.Id}'", eventGridEvent.Id);
            return;
        }

        // Create the Facebook posts for it
        _logger.LogDebug("Looking for source with fields '{tableEvent.PartitionKey}' and '{tableEvent.RowKey}'", tableEvent.PartitionKey, tableEvent.RowKey);
        var sourceData = await _sourceDataRepository.GetAsync(tableEvent.PartitionKey, tableEvent.RowKey);

        if (sourceData == null)
        {
            _logger.LogWarning("Record for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}' was NOT found", tableEvent.PartitionKey, tableEvent.RowKey);
            return;
        }
            
        _logger.LogDebug("Composing Facebook status for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}'.", tableEvent.PartitionKey, tableEvent.RowKey);
            
        var status = ComposeStatus(sourceData);
        if (status != null)
        {
            outboundMessages.Add(status);
        }
            
        // Done
        _logger.LogDebug("Done composing Facebook status for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}'.", tableEvent.PartitionKey, tableEvent.RowKey);
    }
        
    private FacebookPostStatus ComposeStatus(SourceData sourceData)
    {
        if (sourceData == null)
        {
            return null;
        }

        const int maxFacebookStatusText = 2000;
            
        // Build Facebook Status
        var statusText = sourceData.SourceSystem switch
        {
            nameof(SourceSystems.SyndicationFeed) => sourceData.UpdatedOnDate > sourceData.PublicationDate
                ? "Updated Blog Post: "
                : "New Blog Post: ",
            nameof(SourceSystems.YouTube) => sourceData.UpdatedOnDate > sourceData.PublicationDate
                ? "Updated Video: "
                : "New Video: ",
            _ => ""
        };

        var url = sourceData.ShortenedUrl ?? sourceData.Url;
        var postTitle = sourceData.Title;
        var hashTagList = HashTagList(sourceData.Tags);
        
        if (statusText.Length + url.Length + postTitle.Length + 3 + hashTagList.Length >= maxFacebookStatusText)
        {
            var newLength = maxFacebookStatusText - statusText.Length - url.Length - hashTagList.Length - 1;
            postTitle = postTitle.Substring(0, newLength - 4) + "...";
        }
            
        var facebookPostStatus = new FacebookPostStatus
        {
            StatusText =  $"{statusText} {postTitle} {hashTagList}",
            LinkUri = url                
        };

        _logger.LogDebug(
            "Composed Facebook Status: StatusText='{facebookPostStatus.StatusText}', LinkUrl='{facebookPostStatus.LinkUri}'",
            facebookPostStatus.StatusText, facebookPostStatus.LinkUri);
        return facebookPostStatus;
    }
        
    private string HashTagList(string tags)
    {
        if (string.IsNullOrEmpty(tags))
        {
            return "#dotnet #csharp #dotnetcore";
        }

        var tagList = tags.Split(',');
        var hashTagCategories = tagList.Where(tag => !tag.Contains("Article"));

        return hashTagCategories.Aggregate("",
            (current, tag) => current + $" #{tag.Replace(" ", "").Replace(".", "")}");
    }
}