using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class ProcessNewSourceData
{
    private readonly SourceDataRepository _sourceDataRepository;
    private readonly ILogger<ProcessNewSourceData> _logger;
    private readonly ILinkedInApplicationSettings _linkedInApplicationSettings;

    public ProcessNewSourceData(SourceDataRepository sourceDataRepository, ILinkedInApplicationSettings linkedInApplicationSettings, ILogger<ProcessNewSourceData> logger)
    {
        _sourceDataRepository = sourceDataRepository;
        _linkedInApplicationSettings = linkedInApplicationSettings;
        _logger = logger;
    }
    
    // Debug Locally: https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
    // Sample Code: https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events
    // When debugging locally start ngrok
    // Create a new EventGrid endpoint in Azure similar to
    // `https://9ccb49e057a0.ngrok.io/runtime/webhooks/EventGrid?functionName=facebook_process_new_source_data`
    [FunctionName("linkedin_process_new_source_data")]
    public async Task RunAsync(
        [EventGridTrigger()] EventGridEvent eventGridEvent,
        [Queue(Constants.Queues.LinkedInPostLink)] 
        ICollector<LinkedInPostLink> outboundMessages)
    {
        
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.LinkedInProcessNewSourceData, startedAt);
        
        // Get the Source Data identifier for the event
        if (eventGridEvent.Data is null)
        {
            _logger.LogError("The event data was null for event '{Id}'", eventGridEvent.Id);
            return;
        }

        var eventGridData = eventGridEvent.Data.ToString();
        if (eventGridData is null)
        {
            _logger.LogError("Failed to retrieve the value of the eventGrid for event '{Id}'", eventGridEvent.Id);
            return;
        }
        
        var tableEvent = JsonSerializer.Deserialize<TableEvent>(eventGridData);
        if (tableEvent == null)
        {
            _logger.LogError("Failed to parse the TableEvent data for event '{Id}'", eventGridEvent.Id);
            return;
        }

        // Create the LinkedIn posts for it
        _logger.LogDebug("Looking for source with fields '{PartitionKey}' and '{RowKey}'", tableEvent.PartitionKey, tableEvent.RowKey);
        var sourceData = await _sourceDataRepository.GetAsync(tableEvent.PartitionKey, tableEvent.RowKey);

        if (sourceData == null)
        {
            _logger.LogWarning("Record for '{PartitionKey}', '{RowKey}' was NOT found", tableEvent.PartitionKey, tableEvent.RowKey);
            return;
        }
            
        _logger.LogDebug("Composing LinkedIn status for '{PartitionKey}', '{RowKey}'", tableEvent.PartitionKey, tableEvent.RowKey);
            
        var status = ComposeStatus(sourceData);
        if (status != null)
        {
            outboundMessages.Add(status);
        }
            
        // Done
        _logger.LogDebug("Done composing LinkedIn status for '{PartitionKey}', '{RowKey}'", tableEvent.PartitionKey, tableEvent.RowKey);
    }
    
    private LinkedInPostLink ComposeStatus(SourceData sourceData)
    {
        if (sourceData == null)
        {
            return null;
        }

        const int maxLinkedInStatusText = 2000;
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
        
        var post = new LinkedInPostLink
        {
            Text = $"{statusText} {sourceData.Title} {HashTagList(sourceData.Tags)}",
            Title = sourceData.Title,
            LinkUrl = sourceData.Url,
            AuthorId = _linkedInApplicationSettings.AuthorId,
            AccessToken = _linkedInApplicationSettings.AccessToken
        };

        _logger.LogDebug("Composed LinkedIn status for '{PartitionKey}', '{RowKey}', '{@Post}'", sourceData.PartitionKey, sourceData.RowKey, post);
        
        return post;
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