# Infrastructure Needs

A list of all the infrastructure required for this

## Server

App Service (jjgnet) with an Application Insights service

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
| LinkedIn   | `linkedin_process_new_source_data`      | Generates a queue message based on a *New Source* event being triggered | `JosephGuadagno.Broadcasting`           | `LinkedIn.ProcessNewSourceData`       |
| LinkedIn   | `linkedin_post_text`                    | Sends a tweet from **linkedin-post-text** queue                         | `JosephGuadagno.Broadcasting.Functions` | `LinkedIn.PostText`                   |
| LinkedIn   | `linkedin_post_link`                    | Sends a tweet from **linkedin-post-link** queue                         | `JosephGuadagno.Broadcasting.Functions` | `LinkedIn.PostLink`                   |
| LinkedIn   | `linkedin_post_image`                   | Sends a tweet from **linkedin-post-image** queue                        | `JosephGuadagno.Broadcasting.Functions` | `LinkedIn.PostImage`                  |
| LinkedIn   | `linkedin_refresh_tokens`               | Proactively refreshes LinkedIn access token using stored refresh token (5-day buffer, daily check) | `JosephGuadagno.Broadcasting.Functions` | `LinkedIn.RefreshTokens` |

### Queues

* twitter-tweets-to-send
* facebook-post-status-to-page
* linkedin-post-text
* linkedin-post-link
* linkedin-post-image

### Storage - Blob

* jjgnet (holds the functions)

### Storage - Table

* Logging

### Event Grid Topics

Topics are defined as constants in `JosephGuadagno.Broadcasting.Domain.Constants.Topics` ([topics.cs](./src/JosephGuadagno.Broadcasting.Domain/Constants/Topics.cs)) and provisioned via the Aspire AppHost.

Each topic delivers events to Bluesky, Facebook, LinkedIn, and Twitter Azure Functions subscribers.

#### `new-random-post`

Fired when a random post is selected for broadcast.

| Subscriber Function             | Platform  |
|---------------------------------|-----------|
| `BlueskyProcessRandomPostFired` | Bluesky   |
| `FacebookProcessNewRandomPost`  | Facebook  |
| `LinkedInProcessNewRandomPost`  | LinkedIn  |
| `TwitterProcessRandomPostFired` | Twitter   |

#### `new-speaking-engagement`

Fired when a new speaking engagement is collected.

| Subscriber Function                         | Platform  |
|---------------------------------------------|-----------|
| `BlueskyProcessSpeakingEngagementDataFired` | Bluesky   |
| `FacebookProcessSpeakingEngagementDataFired`| Facebook  |
| `LinkedInProcessSpeakingEngagementDataFired`| LinkedIn  |
| `TwitterProcessSpeakingEngagementDataFired` | Twitter   |

#### `new-syndication-feed-item`

Fired when a new syndication feed item is collected.

| Subscriber Function                    | Platform  |
|----------------------------------------|-----------|
| `BlueskyProcessNewSyndicationDataFired`| Bluesky   |
| `FacebookProcessNewSyndicationDataFired`| Facebook |
| `LinkedInProcessNewSyndicationDataFired`| LinkedIn |
| `TwitterProcessNewSyndicationDataFired`| Twitter   |

#### `new-youtube-item`

Fired when a new YouTube video is collected.

| Subscriber Function                 | Platform  |
|-------------------------------------|-----------|
| `BlueskyProcessNewYouTubeDataFired` | Bluesky   |
| `FacebookProcessNewYouTubeDataFired`| Facebook  |
| `LinkedInProcessNewYouTubeDataFired`| LinkedIn  |
| `TwitterProcessNewYouTubeDataFired` | Twitter   |

#### `scheduled-item-fired`

Fired when a scheduled broadcast item is due.

| Subscriber Function                  | Platform  |
|--------------------------------------|-----------|
| `BlueskyProcessScheduledItemFired`   | Bluesky   |
| `FacebookProcessScheduledItemFired`  | Facebook  |
| `LinkedInProcessScheduledItemFired`  | LinkedIn  |
| `TwitterProcessScheduledItemFired`   | Twitter   |

## Database

SQL Server

### Create Script - Database and Users

Location in file [database-create.sql](scripts/database-create.sql)

### Create Script - Tables

Located in [table-create.sql](scripts/table-create.sql)
