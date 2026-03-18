# Decision: All 5 Event Grid Topics in AppHost

**Date:** 2026-03-18
**Author:** Cypher (DevOps Engineer)

## Summary

Added all 5 Event Grid topics from `JosephGuadagno.Broadcasting.Domain.Constants.Topics` to the Aspire AppHost for Azure provisioning.

## Topics Provisioned

| Topic Name                | Constant                |
|---------------------------|-------------------------|
| `new-random-post`         | `Topics.NewRandomPost`          |
| `new-speaking-engagement` | `Topics.NewSpeakingEngagement`  |
| `new-syndication-feed-item` | `Topics.NewSyndicationFeedItem` |
| `new-youtube-item`        | `Topics.NewYouTubeItem`         |
| `scheduled-item-fired`    | `Topics.ScheduledItemFired`     |

## Decisions

### 1. `Azure.Provisioning.EventGrid` via `AddAzureInfrastructure`
There is no `Aspire.Hosting.Azure.EventGrid` package. Topics are provisioned using `builder.AddAzureInfrastructure()` with `EventGridTopic` from `Azure.Provisioning.EventGrid` 1.1.0.

### 2. Endpoints wired to Functions; keys are not
`Azure.Provisioning.EventGrid` 1.1.0 does not expose a `GetKeys()` or `listKeys` equivalent. Topic **endpoints** are output via `ProvisioningOutput` and wired to the Functions project as `EventGridTopics__TopicEndpointSettings__{index}__Endpoint` and `TopicName` env vars. **Keys must be set separately** via Azure App Service settings, Key Vault, or azd parameters.

### 3. Local dev unaffected
The `local.settings.json` already has all 5 topics configured for use with the event-grid-simulator. The AppHost additions only affect Azure provisioning via `azd`.

### 4. `infrastructure-needs.md` updated
Replaced the 2 outdated topics (`new-source-data`, `scheduled-item-fired`) with the correct 5 topics including full subscriber function tables.
