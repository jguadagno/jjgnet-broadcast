using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook
{
    public class ProcessNewSourceData
    {
        private readonly SourceDataRepository _sourceDataRepository;

        public ProcessNewSourceData(SourceDataRepository sourceDataRepository)
        {
            _sourceDataRepository = sourceDataRepository;
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
            ICollector<FacebookPostStatus> outboundMessages,
            ILogger log
        )
        {
            // Get the Source Data identifier for the event
            var tableEvent = JsonSerializer.Deserialize<TableEvent>(eventGridEvent.Data.ToString());
            if (tableEvent == null)
            {
                log.LogError($"Failed to parse the TableEvent data for event '{eventGridEvent.Id}'");
                return;
            }

            // Create the scheduled tweets for it
            log.LogDebug($"Looking for source with fields '{tableEvent.PartitionKey}' and '{tableEvent.RowKey}'");
            var sourceData = await _sourceDataRepository.GetAsync(tableEvent.PartitionKey, tableEvent.RowKey);

            if (sourceData == null)
            {
                log.LogDebug($"Record for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}' was NOT found");
                return;
            }
            
            log.LogDebug($"Composing tweet for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}'.");
            
            var status = ComposeStatus(sourceData);
            if (status != null)
            {
                outboundMessages.Add(status);
            }
            
            // Done
            log.LogDebug($"Done with record for '{tableEvent.PartitionKey}', '{tableEvent.RowKey}'.");
        }
        
        private FacebookPostStatus ComposeStatus(SourceData item)
        {
            if (item == null)
            {
                return null;
            }

            const int maxStatusText = 2000;
            
            // Build Facebook Status
            // Build Tweet
            var statusText = "";
            switch (item.SourceSystem)
            {
                case nameof(SourceSystems.SyndicationFeed):
                    statusText = "New Blog Post: ";
                    break;
                case nameof(SourceSystems.YouTube):
                    statusText = "New Video: ";
                    break;
            }
            
            var url = item.ShortenedUrl ?? item.Url;
            var postTitle = item.Title;
        
            if (statusText.Length + url.Length + postTitle.Length + 3 >= maxStatusText)
            {
                var newLength = maxStatusText - statusText.Length - url.Length - 1;
                postTitle = postTitle.Substring(0, newLength - 4) + "...";
            }
            
            return new FacebookPostStatus
            {
                StatusText =  $"{statusText} {postTitle}",
                LinkUri = url                
            };
        }
    }
}