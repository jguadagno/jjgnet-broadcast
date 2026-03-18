# Trinity Decision Note: ImageUrl Support in Queue Payloads (S4-3)

## Date
2025-01-27

## Context
Issue #269 added `ImageUrl` to `ScheduledItem` (domain + DB column) and exposed it in Scriban templates. However, the queue message models for all 4 platforms did not carry the field, and each platform's sender function logged "ImageUrl not supported" instead of using it. This work closes that gap.

---

## What Was Implemented Per Platform

### Twitter

**Queue model**: Created new `TwitterTweetMessage` (in `Domain.Models.Messages`) with `Text` and `ImageUrl` properties, replacing the plain `string` queue payload.

**Sender functions updated** to return `TwitterTweetMessage?`:
- `Twitter/ProcessScheduledItemFired.cs` — sets `ImageUrl = scheduledItem.ImageUrl`
- `Twitter/ProcessNewSyndicationDataFired.cs` — wraps text in `TwitterTweetMessage { Text = ... }`
- `Twitter/ProcessNewYouTubeData.cs` — same
- `Twitter/ProcessNewRandomPost.cs` — same (no ImageUrl source in these flows)

**Receiver** (`Twitter/SendTweet.cs`): Now accepts `TwitterTweetMessage` instead of `string`. When `ImageUrl` is set, logs a warning that Twitter media API upload is not yet implemented and posts the tweet text without an image attachment.

**Deferred**: Actual image attachment via the Twitter v1.1 media API (`POST media/upload`) is not implemented. The current `ITwitterManager`/`TwitterManager` (LinqToTwitter) only calls `SendTweetAsync(string text)`. Full attachment would require: download image bytes → POST to `media/upload` → get `media_id` → pass `media_ids` in tweet POST.

---

### Facebook

**Queue model**: Added `ImageUrl?` to `FacebookPostStatus` (in `Domain.Models.Messages`).

**Sender function** (`Facebook/ProcessScheduledItemFired.cs`): Sets `facebookPostStatus.ImageUrl = scheduledItem.ImageUrl`. Non-scheduled senders leave `ImageUrl = null`.

**Manager**: Added `PostMessageLinkAndPictureToPage(message, link, picture)` to `IFacebookManager` and `FacebookManager`. This appends `&picture={encoded_url}` to the Graph API `/feed` POST. Facebook uses this parameter as the link-preview thumbnail override.

**Receiver** (`Facebook/PostPageStatus.cs`): When `ImageUrl` is set, calls `PostMessageLinkAndPictureToPage`; otherwise calls `PostMessageAndLinkToPage` (unchanged).

**Note**: The Graph API `picture` parameter overrides the link thumbnail (OG image) in the feed post preview. It does not create a separate "photo post" — that would require `/{page_id}/photos`. The current approach is the simplest integration that attaches an image to a link post without breaking the existing flow.

---

### LinkedIn

**Queue model**: Added `ImageUrl?` to `LinkedInPostLink` (in `Domain.Models.Messages`).

**Sender function** (`LinkedIn/ProcessScheduledItemFired.cs`): Sets `linkedInPost.ImageUrl = scheduledItem.ImageUrl`. Non-scheduled senders leave `ImageUrl = null`.

**Receiver** (`LinkedIn/PostLink.cs`):
- Added `HttpClient httpClient` to constructor (consistent with existing `PostImage.cs`).
- When `ImageUrl` is set: downloads image bytes via `HttpClient`, calls `PostShareTextAndImage` (existing `ILinkedInManager` method) — this is a full image post.
- On image download failure: logs error and falls back to `PostShareTextAndLink`.
- When `ImageUrl` is null: calls `PostShareTextAndLink` (unchanged behavior).

**No manager changes required** — `ILinkedInManager.PostShareTextAndImage` was already present.

---

### Bluesky

**Queue model**: Added `ImageUrl?` to `BlueskyPostMessage` (in `Managers.Bluesky.Models`).

**Sender function** (`Bluesky/ProcessScheduledItemFired.cs`):
- **Breaking fix**: Changed return type from `string?` to `BlueskyPostMessage?`. The original code sent a plain `string` to the queue but `SendPost.cs` expected `BlueskyPostMessage` — a pre-existing type mismatch that would cause runtime deserialization failures.
- Now returns `BlueskyPostMessage { Text = ..., Url = sourceUrl, ImageUrl = scheduledItem.ImageUrl }`.
- Added `GetSourceUrlAsync()` helper to fetch the canonical URL from the source item (used by the embed path).

**Manager**: Added `GetEmbeddedExternalRecordWithThumbnail(externalUrl, thumbnailImageUrl)` to `IBlueskyManager` and `BlueskyManager`. Behaves like `GetEmbeddedExternalRecord` but skips the og:image fetch from the page and instead downloads `thumbnailImageUrl` directly to upload as the card blob thumbnail.

**Receiver** (`Bluesky/SendPost.cs`):
- When `ShortenedUrl` + `Url` are set AND `ImageUrl` is set: uses `GetEmbeddedExternalRecordWithThumbnail(Url, ImageUrl)` to build the link card with the explicit thumbnail.
- When `ShortenedUrl` + `Url` are set, no `ImageUrl`: uses `GetEmbeddedExternalRecord(Url)` (original behavior).
- When `Url` + `ImageUrl` are set (no `ShortenedUrl`): uses `GetEmbeddedExternalRecordWithThumbnail(Url, ImageUrl)` — this covers the scheduled-item path.

**Deferred**: Standalone image embedding (Bluesky `app.bsky.embed.images` record type) — posting an image without a link card — would require a new `IBlueskyManager.UploadImageAndEmbed(imageUrl)` method that uploads the blob and builds an `EmbedImages` record for the `PostBuilder`. Not implemented as the current use case always has a source URL.

---

## Manager Capability Gaps Discovered

| Platform  | Gap                                                                                                           | Effort to close                                                                |
|-----------|---------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------|
| Twitter   | `ITwitterManager.SendTweetAsync` only accepts text; no media upload                                           | Extend with `SendTweetWithImageAsync(text, imageUrl)` using LinqToTwitter media API |
| Facebook  | `PostMessageLinkAndPictureToPage` uses the legacy `picture` param; cannot create a true "photo post" on page | Add `PostPhotoToPage(message, imageUrl)` calling `/{page_id}/photos`           |
| LinkedIn  | ✅ Full image posting already supported via `PostShareTextAndImage`                                            | None                                                                           |
| Bluesky   | No standalone image embed (without a link card)                                                               | Add `UploadImageAndEmbed` to `IBlueskyManager` using `app.bsky.embed.images`  |

## Test Fixes

- `Twitter/ProcessScheduledItemFiredTests.cs`: Updated 5 assertions from `result` (was `string`) to `result?.Text` / `result!.Text` following the `TwitterTweetMessage` return-type change.
- `Bluesky/ProcessScheduledItemFiredTests.cs`: Same pattern for `BlueskyPostMessage`.
