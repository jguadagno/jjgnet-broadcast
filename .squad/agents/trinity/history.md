# Trinity — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Backend Dev
- **Joined:** 2026-03-14T16:37:57.749Z

## Learnings

<!-- Append learnings below -->

### 2026-03-19: Scriban Message Templates Implementation

**Task**: Implement Scriban/Liquid message templates for all 4 social media platforms (Bluesky, Twitter/X, Facebook, LinkedIn) - issues #475-478.

**Approach**:
1. **Discovery Phase**: Analyzed existing hard-coded message construction in all platform Functions:
   - `Bluesky\ProcessNew*.cs` and `ProcessScheduledItemFired.cs`
   - `Twitter\ProcessNew*.cs` and `ProcessScheduledItemFired.cs`
   - `Facebook\ProcessNew*.cs` and `ProcessScheduledItemFired.cs`
   - `LinkedIn\ProcessNew*.cs` (no scheduled item processing found)

2. **Documentation**: Created comprehensive format documentation in `.squad/decisions/inbox/trinity-scriban-templates.md` capturing:
   - Exact message formats for each platform and content type
   - Key differences (RPs vs RTs, URL handling, max lengths)
   - Template variable requirements

3. **Implementation**: Created Liquid templates for each platform:
   - **Bluesky**: 7 templates (3 new content + 4 scheduled items)
   - **Twitter/X**: 7 templates (3 new content + 4 scheduled items)
   - **Facebook**: 7 templates (3 new content + 4 scheduled items)
   - **LinkedIn**: 3 templates (only new content types, no scheduled items in current code)

4. **Branch Strategy**: One feature branch per platform for clean PRs:
   - `feature/s7-bluesky-scriban-templates` → PR #503
   - `feature/s7-twitter-scriban-templates` → PR #504
   - `feature/s7-facebook-scriban-templates` → PR #505
   - `feature/s7-linkedin-scriban-templates` → PR #506

**Key Learnings**:
- **Message Format Variations**: Each platform has subtle but important differences:
  - Bluesky uses "RPs" (reposts), Twitter uses "RTs" (retweets)
  - Bluesky/Twitter embed URLs in message text; Facebook/LinkedIn use separate Link properties
  - Max lengths vary: Bluesky 300, Twitter 240-280, Facebook 2000
  - Date formatting: `.ToShortDateString()` for random posts, `:f` for scheduled events

- **Conditional Logic**: Templates use Scriban conditionals for "Updated" vs "New" prefixes:
  ```liquid
  {{ if item_last_updated_on > publication_date }}Updated Blog Post: {{ else }}New Blog Post: {{ end }}
  ```

- **Scheduled Item Complexity**: Bluesky has ProcessScheduledItemFired already using Scriban with fallback logic (lines 60-95). This shows the integration pattern that will need to be applied to other platforms.

- **Template Organization**: Templates organized by platform in `MessageTemplates/{Platform}/` directories with consistent naming:
  - `new-random-post.liquid`
  - `new-syndication-data.liquid`
  - `new-youtube-data.liquid`
  - `scheduled-item-{type}.liquid`

- **Build Verification**: All builds succeeded with expected warnings (NU1903, NETSDK1206) - no errors introduced.

**Outcome**: Successfully created 24 message templates across 4 platforms, each matching existing hard-coded formats exactly. All PRs opened in Sprint 7 milestone.
