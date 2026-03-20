# Decision: Scriban Template Seeding Strategy (Sprint 7)

**Date:** 2026-03-20  
**Decider:** Trinity (Backend Dev)  
**Epic:** #474 - Templatize all of the messages  
**Issues:** #475 (Bluesky), #476 (Facebook), #477 (LinkedIn), #478 (Twitter)

## Context

The Scriban template infrastructure was implemented in PR #491, adding:
- `MessageTemplate` domain model (Platform, MessageType, Template, Description)
- `IMessageTemplateDataStore` interface with SQL implementation
- Template lookup in all 4 `ProcessScheduledItemFired` functions with fallback to hard-coded messages
- Constants for platforms (Twitter, Facebook, LinkedIn, Bluesky) and message types (RandomPost, NewSyndicationFeedItem, NewYouTubeItem, NewSpeakingEngagement, ScheduledItem)

However, NO templates were seeded in the database, so the system always fell back to the hard-coded message construction.

## Decision

**Seed default Scriban templates via SQL migration script** instead of embedded resource files.

Created `scripts/database/migrations/2026-03-20-seed-message-templates.sql` with 20 templates (5 per platform).

## Options Considered

### Option 1: Database-backed templates (SQL migration) ✅ CHOSEN
**Pros:**
- Can be updated via Web UI (`MessageTemplatesController` already exists)
- No code deployment required to change templates
- Centralized storage in SQL Server (already used for all other configuration)
- Consistent with existing `IMessageTemplateDataStore` implementation

**Cons:**
- Requires database migration execution
- Not version-controlled alongside code (but migrations are)

### Option 2: Embedded resource files (.liquid or .scriban in Functions project)
**Pros:**
- Version-controlled with code
- No database dependency
- Faster lookup (no DB round-trip)

**Cons:**
- Requires code redeployment to update templates
- Would need new loader implementation (file reader)
- Inconsistent with existing `IMessageTemplateDataStore` interface

### Option 3: Azure App Configuration or Key Vault
**Pros:**
- Centralized cloud configuration
- Can be updated without deployment

**Cons:**
- Adds external dependency
- Higher latency than local DB
- More complex than necessary for this use case

## Template Design

### Field Model (Exposed to all templates)
Each platform's `TryRenderTemplateAsync` provides:
- `title`: Post/engagement/talk title
- `url`: Full or shortened URL
- `description`: Comments/engagement details
- `tags`: Space-separated hashtags
- `image_url`: Optional thumbnail URL

### Platform-Specific Templates

#### Bluesky (300 char limit)
- **NewSyndicationFeedItem**: `Blog Post: {{ title }} {{ url }} {{ tags }}`
- **NewYouTubeItem**: `Video: {{ title }} {{ url }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }}) {{ description }}`
- **ScheduledItem**: `My talk: {{ title }} ({{ url }}) {{ description }} Come see it!`
- **RandomPost**: `{{ title }} {{ url }} {{ tags }}`

#### Facebook (2000 char limit, link preview handles URL)
- **NewSyndicationFeedItem**: `ICYMI: Blog Post: {{ title }} {{ tags }}`
- **NewYouTubeItem**: `ICYMI: Video: {{ title }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }})\n\n{{ description }}`
- **ScheduledItem**: `Talk: {{ title }} ({{ url }})\n\n{{ description }}`
- **RandomPost**: `{{ title }}\n\n{{ description }}`

#### LinkedIn (Professional tone)
- **NewSyndicationFeedItem**: `New blog post: {{ title }}\n\n{{ description }}\n\n{{ tags }}`
- **NewYouTubeItem**: `New video: {{ title }}\n\n{{ description }}\n\n{{ tags }}`
- **NewSpeakingEngagement**: `Excited to announce I'll be speaking at {{ title }}!\n\n{{ description }}\n\nLearn more: {{ url }}`
- **ScheduledItem**: `My talk: {{ title }}\n\n{{ description }}\n\nJoin me: {{ url }}`
- **RandomPost**: `{{ title }}\n\n{{ description }}\n\n{{ tags }}`

#### Twitter/X (280 char limit)
- **NewSyndicationFeedItem**: `Blog Post: {{ title }} {{ url }} {{ tags }}`
- **NewYouTubeItem**: `Video: {{ title }} {{ url }} {{ tags }}`
- **NewSpeakingEngagement**: `I'm speaking at {{ title }} ({{ url }}) {{ description }}`
- **ScheduledItem**: `My talk: {{ title }} ({{ url }}) {{ description }} Come see it!`
- **RandomPost**: `{{ title }} {{ url }} {{ tags }}`

## Rationale

1. **Database-backed wins for flexibility**: The Web UI already has a MessageTemplates controller. Admins can tweak templates without code changes.
2. **Simple templates first**: Initial templates mirror the existing hard-coded logic. Future iterations can add Scriban conditionals (`if`/`else`), filters, etc.
3. **Platform limits enforced by code**: Functions already have fallback truncation logic. Templates don't need to handle character limits—they just provide the structure.
4. **Single migration for all platforms**: All 4 platforms share the same infrastructure, so a single SQL file seeds all 20 templates.

## Consequences

### Positive
- Templates are now customizable without redeployment
- Hard-coded fallback logic remains as safety net
- Web UI can manage templates (list, edit, update)
- Future templates can use Scriban's full feature set (conditionals, loops, filters)

### Negative
- Database must be migrated before templates take effect
- Templates are not co-located with code (but migrations are version-controlled)
- No compile-time validation of template syntax (errors logged at runtime)

## Implementation

**Commit:** `6c32c01` (pushed directly to `main`)  
**File:** `scripts/database/migrations/2026-03-20-seed-message-templates.sql`  
**Testing:** Build succeeds (Debug configuration). No unit tests needed for seed data.  
**Deployment:** Run migration script against production SQL Server to activate templates.

## Related

- **Epic:** #474 - Templatize all of the messages
- **Issues:** #475 (Bluesky), #476 (Facebook), #477 (LinkedIn), #478 (Twitter)
- **PR:** #491 - Original template infrastructure implementation
- **Domain Model:** `JosephGuadagno.Broadcasting.Domain.Models.MessageTemplate`
- **Data Store:** `JosephGuadagno.Broadcasting.Data.Sql.MessageTemplateDataStore`
- **Functions:** `ProcessScheduledItemFired` in Twitter, Facebook, LinkedIn, Bluesky folders

## Future Enhancements

1. **Conditional formatting**: Use Scriban `if`/`else` to vary messages based on field values (e.g., "Updated Blog Post" vs "New Blog Post" based on `item_last_updated_on`)
2. **Character limit enforcement in templates**: Add Scriban custom functions to truncate strings at specific lengths
3. **A/B testing**: Store multiple templates per (Platform, MessageType) and randomly select
4. **Localization**: Add a `Language` field to support multi-language templates
5. **Template validation**: Add UI preview/test functionality in the Web app

