<!-- markdownlint-disable MD013 -->
# Scheduled items distributor

The scheduled items path starts with a timer that loads due ScheduledItems rows and groups them by owner. ScheduledItemEventDistributor then loads the referenced content, applies the owner's mappings and templates, and sends queue messages before the function marks the item as sent.

## Flow

```mermaid
flowchart TD
    A[Timer trigger DistributorsScheduledItems] --> B[Load due ScheduledItems from SQL]
    B --> C[Group rows by CreatedByEntraOid]
    C --> D[Load or create FeedChecks row for the owner]
    D --> E[Dispatch each row with ScheduledItemEventDistributor]
    E --> F[Load source content by ScheduledItemType]
    F --> G[Build base SocialMediaPublishRequest]
    G --> H[Load UserEventDistributorMappings for ScheduledItem]
    H --> I[Load MessageTemplates for each mapped platform]
    I --> J[Compose text with PostComposer]
    J --> K[Send SocialMediaPublishRequest to platform queue]
    K --> L[Mark ScheduledItems row as sent]
    L --> M[Update FeedChecks]
```

## Key components

- [`ScheduledItems`](../../src/JosephGuadagno.Broadcasting.Functions/Distributors/ScheduledItems.cs)
- [`ScheduledItemEventDistributor`](../../src/JosephGuadagno.Broadcasting.Functions/Services/ScheduledItemEventDistributor.cs)
- [`ScheduledItems table`](../../scripts/database/table-create.sql)
- [`FeedChecks`](../../scripts/database/table-create.sql)
- [`SyndicationFeedItems`](../../scripts/database/table-create.sql)
- [`YouTubeItems`](../../scripts/database/table-create.sql)
- [`Engagements`](../../scripts/database/table-create.sql) and [`Talks`](../../scripts/database/table-create.sql)
- [`UserEventDistributorMappings`](../../scripts/database/table-create.sql)
- [`MessageTemplates`](../../scripts/database/table-create.sql)
- [`PostComposer`](../../src/JosephGuadagno.Broadcasting.Composers/PostComposer.cs)
- [`SocialMediaPublishRequest`](../../src/JosephGuadagno.Broadcasting.Domain/Models/SocialMediaPublishRequest.cs)
- Azure Queue Storage platform queues

## Related files

- [`ScheduledItems.cs`](../../src/JosephGuadagno.Broadcasting.Functions/Distributors/ScheduledItems.cs)
- [`ScheduledItemEventDistributor.cs`](../../src/JosephGuadagno.Broadcasting.Functions/Services/ScheduledItemEventDistributor.cs)
