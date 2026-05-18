# Decision Record: Phase 3 — Process* Functions Migrated to Template-Based Composition

**Date:** 2026-05-18
**Author:** Neo (Lead/Architect)
**Issue:** #980 — Refactor social media publisher architecture
**Branch:** `issue-980-publisher-architecture-refactor`
**Commit:** `47e8ecec`
**Status:** ✅ COMPLETE

---

## Context

Phase 1 (IPostComposer) and Phase 2 (IMessageTemplateLookup + SocialMediaPublishRequest extension) were already on the branch. Phase 3 migrates all 20 `Process*` Azure Functions away from platform-specific `ComposeMessageAsync()` calls toward the new template-based composition pipeline.

---

## What Changed

### New interface method
`IMessageTemplateDataStore.GetAsync(int socialMediaPlatformId, string messageType, string ownerEntraOid, CancellationToken)` added — user-scoped single-item lookup. Implemented in `Data.Sql/MessageTemplateDataStore.cs` using the same EF Core pattern as the existing non-scoped overload.

### MessageTemplateLookup updated
`MessageTemplateLookup.GetAsync` now calls the user-scoped `GetAsync(platform.Id, messageType, ownerEntraOid, ...)` overload. The Phase 2 TODO comment is removed.

### All 20 Process* functions rewritten
Each function now follows this pipeline:

1. Fetch entity (entity manager call — unchanged)
2. Validate `ownerEntraOid` — bail with warning if null/empty
3. Build `SocialMediaPublishRequest` from entity fields (Title, LinkUrl, ShortenedUrl, Hashtags, ImageUrl)
4. Look up template via `IMessageTemplateLookup.GetAsync(platform, messageType, ownerEntraOid)`
5. Bail with warning (return null, skip enqueue) if no template found
6. Compose text via `IPostComposer.ComposeAsync(request, template.Template)`
7. Bail with warning if composer returns null
8. Return platform-specific queue DTO with composed text

### LinkedIn: OAuth token retained
All LinkedIn `Process*` functions continue to inject `IUserOAuthTokenManager` because `LinkedInPostLink.AccessToken` requires a per-user OAuth token. This moves to `PostLink` function in Phase 5.

### Twitter/Facebook ProcessScheduledItemFired: entity fetch added
Previously these functions only delegated to the platform manager for composition without fetching the underlying entity. Phase 3 adds entity manager injection (`IEngagementManager`, `ISyndicationFeedItemManager`, `IYouTubeItemManager`) and a private `BuildRequestForScheduledItemAsync()` helper, consistent with the Bluesky and LinkedIn patterns.

---

## Key Design Decisions

### Templates are REQUIRED — no fallback
`Process*` functions return `null` (no enqueue) when no template is found for the user+platform+messageType combination. This is a hard requirement from Issue #980 decisions: every user must configure their own templates. Issues #978 (onboarding) and #979 (default templates) ensure users get templates on setup.

### ownerEntraOid source
| Function Type | Source |
|---|---|
| ProcessScheduledItemFired | `scheduledItem.CreatedByEntraOid` |
| ProcessNewSyndicationDataFired | `syndicationFeedItem.CreatedByEntraOid` |
| ProcessNewYouTubeDataFired | `youTubeItem.CreatedByEntraOid` |
| ProcessNewSpeakingEngagementFired | `engagement.CreatedByEntraOid` |
| ProcessNewRandomPost | `syndicationFeedItem.CreatedByEntraOid` |

When `CreatedByEntraOid` is `string?` (nullable — e.g., `ScheduledItem`, `Engagement`): guard with `string.IsNullOrEmpty()`, log warning, return null.

### IList<string> → IReadOnlyCollection<string>
`SyndicationFeedItem.Tags` and `YouTubeItem.Tags` are `IList<string>`. `SocialMediaPublishRequest.Hashtags` is `IReadOnlyCollection<string>?`. Despite `IList<T>` implementing `IReadOnlyCollection<T>`, C# does not allow implicit assignment of a variable typed as `IList<string>` to `IReadOnlyCollection<string>?`. Call `.ToList()` to materialize before assigning.

### Queue DTOs stay platform-specific (Phase 6)
`BlueskyPostMessage`, `TwitterTweetMessage`, `FacebookPostStatus`, `LinkedInPostLink` are not unified yet. Phase 6 will replace all with `SocialMediaPublishRequest` directly.

---

## Tests Updated

All four `ProcessScheduledItemFiredTests.cs` files rewritten:
- Platform manager mocks (`IBlueskyManager`, `ITwitterManager`, `IFacebookManager`, `ILinkedInManager`) removed
- `IMessageTemplateLookup` and `IPostComposer` mocks added
- Tests cover: happy path, null template (→ null result), null composed text (→ null result), entity URL propagation, ImageUrl propagation
- LinkedIn tests: null OAuth token → null result

**Result:** 155 Functions tests passing, all other suites clean.
