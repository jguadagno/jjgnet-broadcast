---
name: "scheduled-item-direct-routing"
description: "Migrate scheduled one-shot publisher flows off Event Grid and onto per-user queue routing."
domain: "azure-functions"
confidence: "high"
source: "earned"
---

# Scheduled item direct routing

## Context

Use this when a timer-driven scheduled publisher currently fires a
single Event Grid event that is then fanned out by platform-specific
bridge functions. In JJGNet Broadcasting, the better pattern is to route
directly to the owner's active `UserEventPublisherMapping` targets and
delete the bridge layer.

## Patterns

- Keep the timer trigger responsible for orchestration only: load due
  items, call a dedicated routing service, mark successful items as
  sent, and update `FeedCheck`.
- Mirror `CollectorEventPublisher` for the routing service: look up
  `GetByUserAndEventTypeAsync(ownerOid,
  MessageTemplates.MessageTypes.ScheduledItem)`, resolve the platform
  template with the int overload of
  `IMessageTemplateManager.GetAsync(...)`, compose the post, and enqueue
  a fresh `SocialMediaPublishRequest` per platform.
- Reuse the shared platform-to-queue dictionary for Twitter, Bluesky,
  LinkedIn, and Facebook.
- Build the publish request from the scheduled item's source type
  (`SyndicationFeedItems`, `YouTubeItems`, `Engagements`, `Talks`)
  inside the routing service, not inside the timer trigger.
- Keep dispatch sequential; do not introduce `Task.WhenAll` around
  scoped manager or data-store calls.
- Delete the dead Event Grid bridge functions, their unit tests, and any
  simulator topic entries that point at those deleted functions.

## Examples

- `src\JosephGuadagno.Broadcasting.Functions\Publishers\ScheduledItems.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Services\ScheduledItemEventPublisher.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Publishers\ScheduledItemsTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Services\ScheduledItemEventPublisherTests.cs`

## Anti-Patterns

- Do not push per-platform dispatch logic back into the timer-trigger
  method body.
- Do not keep `IEventPublisher` wired in the Functions host once
  `ScheduledItems` is fully off Event Grid.
- Do not leave deleted function names in the Event Grid simulator
  config.
- Do not share one mutable `SocialMediaPublishRequest` instance across
  multiple platform sends.
