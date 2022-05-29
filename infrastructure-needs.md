# Infrastructure Needs

A list of all the infrastructure required for this

## Server

App Service (jjgnet) with an Application Insights services

US West 2 - Type (P1v2)

### API

api-jjgnet-broadcast

### Web

web-jjgnet-broadcast

### Function


| Category   | Name                                    | Purpose                                                                 | Project                                 | Class                                 |
|------------|-----------------------------------------|-------------------------------------------------------------------------|-----------------------------------------|---------------------------------------|
| Collectors | `collectors_feed_check_for_updates`     | Gets new posts from xml feed                                            | `JosephGuadagno.Broadcasting.Functions` | `Collectors.CheckFeedForUpdates`      |
| Collectors | `collectors_feed_load_json_feed_items`  | Gets all posts from Json feed                                           | `JosephGuadagno.Broadcasting.Functions` | `LoadJsonFeedItems`                   |
| Collectors | `collectors_youtube_load_new_videos`    | Gets new videos from YouTube channel                                    | `JosephGuadagno.Broadcasting.Functions` | `Collectors.YouTube.LoadNewVideos`    |
| Collectors | `collectors_youtube_load_all_videos`    | Gets all videos from a YouTube channel                                  | `JosephGuadagno.Broadcasting.Functions` | `Collectors.YouTube.LoadAllVideos`    |
| Twitter    | `twitter_process_new_source_data`       | Generates a queue message based on a *New Source* event being triggered | `JosephGuadagno.Broadcasting`           | `Twitter.ProcessNewSourceData`        |
| Twitter    | `twitter_send_tweet`                    | Sends a tweet from **twitter-tweets-to-send** queue                     | `JosephGuadagno.Broadcasting.Functions` | `Twitter.SendTweet`                   |
| Twitter    | `twitter_process_scheduled_item_fired`  | Triggered when there is a scheduled item to go to Twitter               | `JosephGuadagno.Broadcasting.Functions` | `Twitter.ProcessScheduledItemsFired`  |
| Facebook   | `facebook_process_new_source_data`      | Generates a queue message based on a *New Source* event being triggered | `JosephGuadagno.Broadcasting`           | `Facebook.ProcessNewSourceData`       |
| Facebook   | `facebook_post_status_to_page`          | Sends a tweet from **facebook-post-status-to-page** queue               | `JosephGuadagno.Broadcasting.Functions` | `Facebook.PostPageStatus`             |
| Facebook   | `facebook_process_scheduled_item_fired` | Triggered when there is a scheduled item to go to Facebook              | `JosephGuadagno.Broadcasting.Functions` | `Facebook.ProcessScheduledItemsFired` |

### Queues

* twitter-tweets-to-send
* facebook-post-status-to-page

### Storage - Blob

* jjgnet (hold the functions)

### Storage - Table

* Configuration
* SourceData
* Logging

### Event Grid Topics

#### New Source Data

Name: `new-source-data`

Topic Endpoint: `https://new-source-data.westus2-1.eventgrid.azure.net/api/events`

[Azure Resource Manager](https://new-source-data.westus2-1.eventgrid.azure.net/api/events)

##### Event Grid - Twitter

| Name          | Value                             | Description |
|---------------|-----------------------------------|-------------|
| Name          | `source-data-to-twitter`          |             |
| Event Schema  | `Event Grid Scheme`               |             |
| Endpoint Type | `Azure Functions`                 |             |
| Endpoint      | `twitter-process-new-source-data` |             |

##### Event Grid - Facebook

| Name          | Value                              | Description |
|---------------|------------------------------------|-------------|
| Name          | `source-data-to-facebook`          |             |
| Event Schema  | `Event Grid Scheme`                |             |
| Endpoint Type | `Azure Functions`                  |             |
| Endpoint      | `facebook-process-new-source-data` |             |

#### Scheduled Item Fired

Name: `scheduled-item-fired`

Topic Endpoint: `https://scheduled-item-fired.westus2-1.eventgrid.azure.net/api/events`

[Azure Resource Manager](https://scheduled-item-fired.westus2-1.eventgrid.azure.net/api/events)

##### Event Grid - Twitter

| Name          | Value                                  | Description |
|---------------|----------------------------------------|-------------|
| Name          | `scheduled-item-to-twitter`            |             |
| Event Schema  | `Event Grid Scheme`                    |             |
| Endpoint Type | `Azure Functions`                      |             |
| Endpoint      | `twitter-process-scheduled-item-fired` |             |

##### Event Grid - Facebook

| Name          | Value                                   | Description |
|---------------|-----------------------------------------|-------------|
| Name          | `scheduled-item-to-facebook`            |             |
| Event Schema  | `Event Grid Scheme`                     |             |
| Endpoint Type | `Azure Functions`                       |             |
| Endpoint      | `facebook-process-scheduled-item-fired` |             |


## Database

SQL Server

### Create Script - Database and Users

Location in file [database-create.sql](scripts/database-create.sql)

### Create Script - Tables

Located in [table-create.sql](scripts/table-create.sql)