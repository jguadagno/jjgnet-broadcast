# Infrastructure Needs

A list of all of the infrastructure required for this

## Server

App Service (jjgnet) with an Application Insights services

### Function

|---|---|---|---|
| Name | Purpose | Project | Class |
| `collector_feed` | Gets new posts from xml feed | `JosephGuadagno.Broadcasting.Collectors.Functions` | `FeedCollector` |
| `twitter_send_tweet` | Sends a tweet | `JosephGuadagno.Broadcasting.Twitter.Functions` | `SendTweet` |

### Queues

* twitter-tweets-to-send

### Storage - Blob

* jjgnet (hold the functions)

### Storage - Table

* Configuration
* SourceData

### Event Grid Topics

#### New Source Data

Name: `new-source-data`

Topic Endpoint: `https://new-source-data.westus2-1.eventgrid.azure.net/api/events`

[Azure Resource Manager](https://new-source-data.westus2-1.eventgrid.azure.net/api/events)

|---|---|---|
| Name | Value | Description |
| Name | `source-data-to-twitter` | |
| Event Schema | `Event Grid Scheme` | |
| Endpoint Type | `Azure Functions` | |
| Endpoint | `twitter-process-new-source-data` | |
