// eventgrid.bicep — Event Grid topics and subscriptions
//
// Topics:
//   new-source-data          → subscriptions to Twitter, Facebook, LinkedIn Functions
//   scheduled-item-fired     → subscriptions to Twitter, Facebook, LinkedIn Functions
//
// NOTE: Event Grid subscriptions to Azure Functions require the Function App to already
// exist and the function endpoints to be registered. The subscription webhook URLs are
// derived from the Function App's host key at deployment time.

@description('Location for all resources')
param location string

@description('Resource tags to apply to all resources')
param tags object = {}

@description('Name of the Functions App (used to build webhook URLs)')
param functionAppName string

// Retrieve the existing Function App to get its keys
resource functionApp 'Microsoft.Web/sites@2023-12-01' existing = {
  name: functionAppName
}

// --- new-source-data topic ---
resource newSourceDataTopic 'Microsoft.EventGrid/topics@2023-12-15-preview' = {
  name: 'new-source-data'
  location: location
  tags: tags
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

// --- scheduled-item-fired topic ---
resource scheduledItemFiredTopic 'Microsoft.EventGrid/topics@2023-12-15-preview' = {
  name: 'scheduled-item-fired'
  location: location
  tags: tags
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

// Helper: build the Azure Functions webhook URL for an Event Grid-triggered function
// The system key 'eventgrid_extension' is the standard key used by the Event Grid extension.
var functionBaseUrl = 'https://${functionApp.properties.defaultHostName}/runtime/webhooks/EventGrid'
var eventGridSystemKey = listKeys('${functionApp.id}/host/default', '2023-12-01').systemKeys.eventgrid_extension

// --- new-source-data subscriptions ---
resource subscriptionSourceToTwitter 'Microsoft.EventGrid/topics/eventSubscriptions@2023-12-15-preview' = {
  parent: newSourceDataTopic
  name: 'source-data-to-twitter'
  properties: {
    eventDeliverySchema: 'EventGridSchema'
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: '${functionBaseUrl}?functionName=twitter_process_new_source_data&code=${eventGridSystemKey}'
      }
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

resource subscriptionSourceToFacebook 'Microsoft.EventGrid/topics/eventSubscriptions@2023-12-15-preview' = {
  parent: newSourceDataTopic
  name: 'source-data-to-facebook'
  properties: {
    eventDeliverySchema: 'EventGridSchema'
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: '${functionBaseUrl}?functionName=facebook_process_new_source_data&code=${eventGridSystemKey}'
      }
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

resource subscriptionSourceToLinkedIn 'Microsoft.EventGrid/topics/eventSubscriptions@2023-12-15-preview' = {
  parent: newSourceDataTopic
  name: 'source-data-to-linkedin'
  properties: {
    eventDeliverySchema: 'EventGridSchema'
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: '${functionBaseUrl}?functionName=linkedin_process_new_source_data&code=${eventGridSystemKey}'
      }
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

// --- scheduled-item-fired subscriptions ---
resource subscriptionScheduledToTwitter 'Microsoft.EventGrid/topics/eventSubscriptions@2023-12-15-preview' = {
  parent: scheduledItemFiredTopic
  name: 'scheduled-item-to-twitter'
  properties: {
    eventDeliverySchema: 'EventGridSchema'
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: '${functionBaseUrl}?functionName=twitter_process_scheduled_item_fired&code=${eventGridSystemKey}'
      }
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

resource subscriptionScheduledToFacebook 'Microsoft.EventGrid/topics/eventSubscriptions@2023-12-15-preview' = {
  parent: scheduledItemFiredTopic
  name: 'scheduled-item-to-facebook'
  properties: {
    eventDeliverySchema: 'EventGridSchema'
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: '${functionBaseUrl}?functionName=facebook_process_scheduled_item_fired&code=${eventGridSystemKey}'
      }
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

resource subscriptionScheduledToLinkedIn 'Microsoft.EventGrid/topics/eventSubscriptions@2023-12-15-preview' = {
  parent: scheduledItemFiredTopic
  name: 'scheduled-item-to-linkedin'
  properties: {
    eventDeliverySchema: 'EventGridSchema'
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: '${functionBaseUrl}?functionName=linkedin_process_scheduled_item_fired&code=${eventGridSystemKey}'
      }
    }
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

output newSourceDataTopicEndpoint string = newSourceDataTopic.properties.endpoint
output newSourceDataTopicId string = newSourceDataTopic.id
output scheduledItemFiredTopicEndpoint string = scheduledItemFiredTopic.properties.endpoint
output scheduledItemFiredTopicId string = scheduledItemFiredTopic.id
