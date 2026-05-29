<!-- markdownlint-disable MD013 -->
# Collector event dispatcher

CollectorEventDispatcher handles the fan-out after a collector saves a new feed item, video, or speaking engagement. It looks up the owner's active routing, renders platform-specific text with PostComposer, and writes a fresh SocialMediaPublishRequest to the correct queue.

## Flow

```mermaid
flowchart TD
    A[Saved collector item arrives] --> B[Build base SocialMediaPublishRequest from item data]
    B --> C[Load UserEventDispatcherMappings for owner and event type]
    C --> D{Any active mappings?}
    D -- No --> E[Stop]
    D -- Yes --> F[For each mapped platform, load MessageTemplates]
    F --> G[Compose text with PostComposer]
    G --> H{Text created?}
    H -- No --> I[Skip platform]
    H -- Yes --> J[Resolve platform queue name]
    J --> K[Create queue if needed]
    K --> L[Send SocialMediaPublishRequest JSON to Azure Queue Storage]
```

## Key components

- [`CollectorEventDispatcher`](../../src/JosephGuadagno.Broadcasting.Functions/Services/CollectorEventDispatcher.cs)
- [`UserEventDispatcherMappings`](../../scripts/database/table-create.sql)
- [`MessageTemplates`](../../scripts/database/table-create.sql)
- [`PostComposer`](../../src/JosephGuadagno.Broadcasting.Composers/PostComposer.cs)
- [`SocialMediaPublishRequest`](../../src/JosephGuadagno.Broadcasting.Domain/Models/SocialMediaPublishRequest.cs)
- QueueServiceClient
- twitter-tweets-to-send
- bluesky-post-to-send
- linkedin-post-link
- facebook-post-status-to-page

## Related files

- [`CollectorEventDispatcher.cs`](../../src/JosephGuadagno.Broadcasting.Functions/Services/CollectorEventDispatcher.cs)
- [`LoadNewPosts.cs`](../../src/JosephGuadagno.Broadcasting.Functions/Collectors/SyndicationFeed/LoadNewPosts.cs)
- [`LoadNewVideos.cs`](../../src/JosephGuadagno.Broadcasting.Functions/Collectors/YouTube/LoadNewVideos.cs)
- [`LoadNewSpeakingEngagements.cs`](../../src/JosephGuadagno.Broadcasting.Functions/Collectors/SpeakingEngagement/LoadNewSpeakingEngagements.cs)
