# Morpheus Decisions: MessageTemplate Seed Data (Issue S4-4-seed)

## Date
2026-03-18

## Branch
`feature/s4-4-seed-message-templates`

---

## Summary

Added default seed data for the `MessageTemplates` table to `scripts/database/data-create.sql`.
This ensures that when Aspire provisions a fresh database, all 4 platforms × 5 message types
(20 total rows) are pre-populated. Without this, Scriban rendering in the publish Functions
would fall through to hardcoded fallback strings on every send.

---

## Scriban Template Variables (per message type)

All 4 `ProcessScheduledItemFired` Functions populate these fields in `TryRenderTemplateAsync`:

| Variable | Source | Feed/YouTube | Engagements | Talks |
|----------|--------|:---:|:---:|:---:|
| `{{ title }}` | `Title` / `Name` | ✅ | ✅ | ✅ |
| `{{ url }}` | `ShortenedUrl ?? Url` / `Url` / `UrlForTalk` | ✅ | ✅ | ✅ |
| `{{ description }}` | `Comments` (empty for feed/YouTube) | empty string | `Comments ?? ""` | `Comments` |
| `{{ tags }}` | `Tags ?? ""` (empty for engagements/talks) | `Tags ?? ""` | empty string | empty string |
| `{{ image_url }}` | `ScheduledItem.ImageUrl` (nullable) | ✅ | ✅ | ✅ |

> **Note on `image_url`**: It is passed to the Scriban context but is NOT forwarded to any of the
> 4 platform queue payload types (Twitter/Bluesky use `string?`, Facebook uses `FacebookPostStatus`,
> LinkedIn uses `LinkedInPostLink` — none have an image field). A `LogInformation` is emitted when
> `image_url` is non-null. Image support is a future work item.

---

## Platform-Specific Constraints

| Platform | Character limit | Tone | Notes |
|----------|----------------|------|-------|
| Twitter | ~280 chars | Casual | Templates kept short: `title + url` pattern |
| Bluesky | ~300 chars | Casual | Same length constraints as Twitter |
| Facebook | ~2000 chars | Informal | Multi-line with description block |
| LinkedIn | ~3000 chars | Professional | Multi-line with description block |

---

## Message Types Seeded

| MessageType | Purpose | Currently used in code? |
|-------------|---------|:---:|
| `RandomPost` | Default template for all scheduled items | ✅ Yes (all 4 Functions query this) |
| `NewSyndicationFeedItem` | New RSS/Atom blog post announced | ❌ Reserved for future use |
| `NewYouTubeItem` | New YouTube video announced | ❌ Reserved for future use |
| `NewSpeakingEngagement` | New conference/event speaking slot | ❌ Reserved for future use |
| `ScheduledItem` | Generic scheduled broadcast | ❌ Reserved for future use |

> All 4 Functions currently load only `MessageTypes.RandomPost` (see `MessageTemplates.cs` constants).
> The other 4 types are seeded now so they are ready when the code is extended.

---

## Template Designs

### Twitter & Bluesky (short-form)

| MessageType | Template |
|-------------|----------|
| RandomPost | `{{ title }} - {{ url }}` |
| NewSyndicationFeedItem | `Blog Post: {{ title }} {{ url }}` |
| NewYouTubeItem | `New video: {{ title }} {{ url }}` |
| NewSpeakingEngagement | `I'm speaking at {{ title }}! {{ url }}` (Twitter) / `Speaking at {{ title }}! {{ url }}` (Bluesky) |
| ScheduledItem | `{{ title }} {{ url }}` |

### Facebook (multi-line, informal)

| MessageType | Template |
|-------------|----------|
| RandomPost | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |
| NewSyndicationFeedItem | `ICYMI: {{ title }}\n\n{{ description }}\n\n{{ url }}` |
| NewYouTubeItem | `New video: {{ title }}\n\n{{ description }}\n\nWatch now: {{ url }}` |
| NewSpeakingEngagement | `I'm speaking at {{ title }}!\n\n{{ description }}\n\n{{ url }}` |
| ScheduledItem | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |

### LinkedIn (multi-line, professional)

| MessageType | Template |
|-------------|----------|
| RandomPost | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |
| NewSyndicationFeedItem | `New blog post: {{ title }}\n\n{{ description }}\n\nRead more: {{ url }}` |
| NewYouTubeItem | `New video: {{ title }}\n\n{{ description }}\n\nWatch: {{ url }}` |
| NewSpeakingEngagement | `I am excited to announce I will be speaking at {{ title }}.\n\n{{ description }}\n\nLearn more: {{ url }}` |
| ScheduledItem | `{{ title }}\n\n{{ description }}\n\n{{ url }}` |

---

## Seed Approach

### Why `data-create.sql` (not a migration)?

The Aspire AppHost (`AppHost.cs`) uses `WithCreationScript` which concatenates exactly:
1. `database-create.sql`
2. `table-create.sql`
3. `data-create.sql`

The `scripts/database/migrations/` directory is NOT loaded by Aspire — migrations are manual
one-off scripts for existing databases. Since the `MessageTemplates` table is already defined
in `table-create.sql`, the seed data must go in `data-create.sql` to be provisioned on fresh
database creation.

> **Cross-reference**: The migration file `2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql`
> seeded 4 `RandomPost` templates for existing databases. The new `data-create.sql` entries cover
> all 20 templates for fresh provisioning.

### Idempotency

Each of the 20 inserts is wrapped in an `IF NOT EXISTS` guard:
```sql
IF NOT EXISTS (SELECT 1 FROM JJGNet.dbo.MessageTemplates
               WHERE Platform = N'Twitter' AND MessageType = N'RandomPost')
    INSERT INTO JJGNet.dbo.MessageTemplates ...
```

This makes the seed block re-runnable (e.g., if someone runs `data-create.sql` against an
existing database, or if Aspire's creation script mechanism is ever changed).

### Newlines in multi-line templates

Facebook and LinkedIn templates use SQL Server `CHAR(10)` concatenation for embedded newlines,
matching the pattern established in the existing migration:
```sql
N'{{ title }}' + CHAR(10) + CHAR(10) + N'{{ description }}' + CHAR(10) + CHAR(10) + N'{{ url }}'
```

This produces `\n\n` (double newline) paragraph breaks, which render correctly in social platform
post text fields.
