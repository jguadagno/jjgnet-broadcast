# Trinity Decisions: Scriban Template Rendering in Publish Functions (Issue #269)

## Date
2026-03-17

## Branch
`issue-269` — commit `f924641`

---

## Files Modified

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Functions/JosephGuadagno.Broadcasting.Functions.csproj` | Added `Scriban 6.5.8` NuGet package |
| `src/JosephGuadagno.Broadcasting.Functions/Program.cs` | Registered `IMessageTemplateDataStore` → `MessageTemplateDataStore` as scoped in `ConfigureFunction` |
| `src/JosephGuadagno.Broadcasting.Functions/Twitter/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |
| `src/JosephGuadagno.Broadcasting.Functions/Facebook/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |
| `src/JosephGuadagno.Broadcasting.Functions/LinkedIn/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |
| `src/JosephGuadagno.Broadcasting.Functions/Bluesky/ProcessScheduledItemFired.cs` | Added Scriban rendering + IMessageTemplateDataStore injection |

---

## Scriban Model Field Names

The Scriban template context exposes these fields (populated from the referenced item):

| Field | Source |
|-------|--------|
| `title` | `SyndicationFeedSource.Title` / `YouTubeSource.Title` / `Engagement.Name` / `Talk.Name` |
| `url` | `ShortenedUrl ?? Url` for feed/YouTube; `Engagement.Url`; `Talk.UrlForTalk` |
| `description` | Empty string for feed/YouTube; `Engagement.Comments ?? ""`; `Talk.Comments` |
| `tags` | `feed.Tags ?? ""` / `yt.Tags ?? ""`; empty string for engagement/talk |
| `image_url` | `ScheduledItem.ImageUrl` (nullable) |

Example seed templates (from `scripts/database/migrations/2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql`):
- Twitter/Bluesky: `{{ title }} - {{ url }}`
- Facebook/LinkedIn: `{{ title }}\n\n{{ description }}\n\n{{ url }}`

---

## Fallback Logic (Per Platform)

### Twitter and Bluesky (return `string?`)

```
1. Load template: messageTemplateDataStore.GetAsync("Twitter"/"Bluesky", "RandomPost")
2. If template.Template is not null/whitespace → call TryRenderTemplateAsync
3. If render succeeds (non-null, non-whitespace) → use rendered string as post text
4. If render returns null (no template / error / empty) → existing switch/case fallback runs
   (GetPostForSyndicationSource / GetPostForYouTubeSource / GetPostForEngagement / GetPostForTalk)
```

The existing `GetPost*` helpers are **completely unchanged** and still present as the fallback.

### Facebook (return `FacebookPostStatus?`)

```
1. Always run existing switch → populates facebookPostStatus.StatusText AND .LinkUri
2. Load template: messageTemplateDataStore.GetAsync("Facebook", "RandomPost")
3. If template exists → call TryRenderTemplateAsync
4. If render succeeds → override facebookPostStatus.StatusText with rendered text
5. LinkUri is always from the item (never overridden)
```

Rationale: Facebook requires both a text body AND a link URL. The switch is always needed for LinkUri; the template only replaces the text portion.

### LinkedIn (return `LinkedInPostLink?`)

```
1. Always run existing switch → populates linkedInPost.Title AND .LinkUrl
2. Load template: messageTemplateDataStore.GetAsync("LinkedIn", "RandomPost")
3. If template exists → call TryRenderTemplateAsync → store as renderedText
4. linkedInPost.Text = renderedText ?? scheduledItem.Message
5. AuthorId and AccessToken set from linkedInApplicationSettings as before
```

Fallback is `scheduledItem.Message` (the pre-stored message on the scheduled item), matching the original behavior.

---

## TryRenderTemplateAsync (shared pattern in all 4 functions)

Each function has a private `TryRenderTemplateAsync(ScheduledItem scheduledItem, string templateContent)` method that:

1. Loads the referenced item via the appropriate manager based on `scheduledItem.ItemType`
2. Maps item properties to `title`, `url`, `description`, `tags`
3. Parses and renders via Scriban: `Template.Parse` → `ScriptObject.Import` → `TemplateContext` → `RenderAsync`
4. Returns the trimmed rendered string, or `null` if rendering fails or produces whitespace
5. Any exception is caught, logged as `LogWarning`, and returns `null` (never throws — fallback always available)

---

## ImageUrl Handling Per Platform

`ScheduledItem.ImageUrl` is passed as `image_url` in the Scriban model so templates can include it via `{{ image_url }}`.

For the queue payload (what is placed on the Azure Storage Queue), none of the 4 platform queue message models support an image URL field:

| Platform | Queue message type | ImageUrl support |
|----------|--------------------|-----------------|
| Twitter | `string?` (plain text) | ❌ Not supported in plain string queue message |
| Facebook | `FacebookPostStatus` (StatusText + LinkUri) | ❌ No image field on `FacebookPostStatus` |
| LinkedIn | `LinkedInPostLink` (Text + Title + LinkUrl + AuthorId + AccessToken) | ❌ No image field on `LinkedInPostLink` |
| Bluesky | `string?` (plain text) | ❌ Not supported in plain string queue message |

In all 4 cases, if `scheduledItem.ImageUrl` is not null/empty, a `LogInformation` message is emitted:
> `"ImageUrl '{ImageUrl}' is available for scheduled item {Id} but is not supported in the {Platform} queue payload"`

No exception is thrown and the broadcast proceeds normally. A future issue can add image support when the queue message schemas are extended.

---

## DI Registration

Added to `ConfigureFunction` in `Program.cs`:

```csharp
services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();
```

Placed after the existing `TokenRefresh` registrations. Uses `TryAddScoped` consistent with all other data store registrations in the Functions project.

---

## Build Result

`dotnet build` — **Build succeeded, 0 errors**. All warnings are pre-existing nullable reference / XML doc warnings unrelated to this change.
