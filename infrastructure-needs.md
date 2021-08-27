# Infrastructure Needs

A list of all of the infrastructure required for this

## Server

App Service (jjgnet) with an Application Insights services

### Function


| Category | Name | Purpose | Project | Class |
| --- | ---|---|---|---|
| Collectors | `collectors_feed_check_for_updates` | Gets new posts from xml feed | `JosephGuadagno.Broadcasting.Functions` | `Collectors.CheckFeedForUpdates` |
| Collectors | `collectors_feed_load_json_feed_items` | Gets all posts from Json feed | `JosephGuadagno.Broadcasting.Functions` | `LoadJsonFeedItems` |
| Collectors | `collectors_youtube_load_new_videos` | Gets new videos from YouTube channel | `JosephGuadagno.Broadcasting.Functions` | `Collectors.YouTube.LoadNewVideos` |
| Collectors | `collectors_youtube_load_all_videos` | Gets all videos from a YouTube channel | `JosephGuadagno.Broadcasting.Functions` | `Collectors.YouTube.LoadAllVideos` |
| Twitter | `twitter_process_new_source_data` | Generates a queue message based on a *New Source* event being triggered | `JosephGuadagno.Broadcasting` | `Twitter.ProcessNewSourceData` |
| Twitter | `twitter_send_tweet` | Sends a tweet from **twitter-tweets-to-send** queue | `JosephGuadagno.Broadcasting.Functions` | `Twitter.SendTweet` |
| Facebook | `facebook_process_new_source_data` | Generates a queue message based on a *New Source* event being triggered | `JosephGuadagno.Broadcasting` | `Facebook.ProcessNewSourceData` |
| Facebook | `facebook_post_status_to_page` | Sends a tweet from **facebook-post-status-to-page** queue | `JosephGuadagno.Broadcasting.Functions` | `Facebook.PostPageStatus` |

### Queues

* twitter-tweets-to-send
* facebook-post-status-to-page

### Storage - Blob

* jjgnet (hold the functions)

### Storage - Table

* Configuration
* SourceData
* Engagements
* Talks
* ScheduledItems

### Event Grid Topics

#### New Source Data

Name: `new-source-data`

Topic Endpoint: `https://new-source-data.westus2-1.eventgrid.azure.net/api/events`

[Azure Resource Manager](https://new-source-data.westus2-1.eventgrid.azure.net/api/events)

##### Event Grid - Twitter

| Name | Value | Description |
|---|---|---|
| Name | `source-data-to-twitter` | |
| Event Schema | `Event Grid Scheme` | |
| Endpoint Type | `Azure Functions` | |
| Endpoint | `twitter-process-new-source-data` | |

##### Event Grid - Facebook

| Name | Value | Description |
|---|---|---|
| Name | `source-data-to-facebook` | |
| Event Schema | `Event Grid Scheme` | |
| Endpoint Type | `Azure Functions` | |
| Endpoint | `facebook-process-new-source-data` | |
