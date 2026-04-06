@description('Azure region for the EventGrid topics.')
param location string

@description('Resource tags.')
param tags object = {}

// Discovered EventGrid topics (all in westus2):
//   - new-random-post
//   - new-speaking-engagement
//   - new-syndication-feed-item
//   - new-youtube-item
//   - scheduled-item-fired

resource topicNewRandomPost 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: 'new-random-post'
  location: location
  tags: tags
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

resource topicNewSpeakingEngagement 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: 'new-speaking-engagement'
  location: location
  tags: tags
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

resource topicNewSyndicationFeedItem 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: 'new-syndication-feed-item'
  location: location
  tags: tags
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

resource topicNewYouTubeItem 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: 'new-youtube-item'
  location: location
  tags: tags
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

resource topicScheduledItemFired 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: 'scheduled-item-fired'
  location: location
  tags: tags
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

output topicNewRandomPostId string = topicNewRandomPost.id
output topicNewRandomPostEndpoint string = topicNewRandomPost.properties.endpoint
output topicNewSpeakingEngagementId string = topicNewSpeakingEngagement.id
output topicNewSpeakingEngagementEndpoint string = topicNewSpeakingEngagement.properties.endpoint
output topicNewSyndicationFeedItemId string = topicNewSyndicationFeedItem.id
output topicNewSyndicationFeedItemEndpoint string = topicNewSyndicationFeedItem.properties.endpoint
output topicNewYouTubeItemId string = topicNewYouTubeItem.id
output topicNewYouTubeItemEndpoint string = topicNewYouTubeItem.properties.endpoint
output topicScheduledItemFiredId string = topicScheduledItemFired.id
output topicScheduledItemFiredEndpoint string = topicScheduledItemFired.properties.endpoint
