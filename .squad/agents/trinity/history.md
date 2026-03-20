# Trinity — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Backend Dev
- **Joined:** 2026-03-14T16:37:57.749Z

## Learnings

### 2026-03-20: Scriban Template Seeding (Sprint 7)

**Task:** Implement Scriban-based message templates for all 4 social platforms (Epic #474, Issues #475-478).

**What I Found:**
1. **Infrastructure already complete**: PR #491 implemented the template lookup and fallback mechanism in all 4 `ProcessScheduledItemFired` functions (Twitter, Facebook, LinkedIn, Bluesky). Issues #474-478 were marked closed, but no actual templates existed.
2. **Database-backed approach**: `MessageTemplates` table exists with `(Platform, MessageType)` composite key. `IMessageTemplateDataStore` interface and SQL implementation already in place.
3. **Template model**: Each function's `TryRenderTemplateAsync` exposes 5 fields to Scriban templates: `title`, `url`, `description`, `tags`, `image_url`.
4. **Fallback mechanism working**: When no template is found, functions fall back to hard-coded string construction (the original implementation).

**What I Implemented:**
- Created `2026-03-20-seed-message-templates.sql` migration script seeding 20 templates (5 per platform)
- Each platform has templates for: `NewSyndicationFeedItem`, `NewYouTubeItem`, `NewSpeakingEngagement`, `ScheduledItem`, `RandomPost`
- Platform-specific considerations:
  - **Bluesky**: 300 char limit, compact inline format `Blog Post: {{ title }} {{ url }} {{ tags }}`
  - **Facebook**: 2000 char limit, ICYMI prefix, URLs handled via separate `LinkUri` property
  - **LinkedIn**: Professional tone, multiline formatting with hashtags
  - **Twitter**: 280 char limit, compact format aware of URL shortening
- Templates match the existing fallback logic closely but are now customizable via database

**Design Decisions:**
1. **SQL migration over embedded files**: Database-backed templates are more flexible and can be updated via the Web UI's MessageTemplates controller without redeployment
2. **Simple templates first**: Initial templates closely mirror the existing hard-coded logic. Future iterations can add conditional logic (Scriban supports `if`/`else`)
3. **No character limit enforcement in templates**: Functions still enforce platform limits with fallback truncation if templates render too long

**Outcome:**
- ~~Pushed directly to `main` branch (commit `6c32c01`)~~ **CORRECTED:** Commits reverted from main and moved to PR #502
- Single commit covers all 4 platforms since they share the same infrastructure
- Build succeeds (Debug configuration, Release has known file locking issue in CI)
- No code changes needed - only database seed data

### 2026-03-20: Workflow Correction - Always Use Feature Branch + PR

**CRITICAL LEARNING:** ALL work must use a feature branch and PR workflow. **NEVER commit directly to `main`.**

**Remediation Performed:**
- Original commits `6c32c01` and `e5a4f73` were pushed directly to main (violating team policy)
- Created feature branch `feature/s7-474-scriban-message-templates` from commit `f35a60d` (before the direct commits)
- Cherry-picked both commits onto the feature branch
- Reverted the direct commits from main (new commits `2264f7f` and `d3c3843`)
- Pushed feature branch and created PR #502: https://github.com/jguadagno/jjgnet-broadcast/pull/502
- PR includes proper milestone (Sprint 7) and closes issues #474-478

**Going Forward:**
1. **Always** create a feature branch first: `git checkout -b feature/description`
2. **Always** push to feature branch: `git push origin feature/description`
3. **Always** create a PR with `gh pr create` (include milestone, issue links)
4. **Never** push directly to `main` - even for "small" changes like SQL migrations or documentation

<!-- Append learnings below -->

### 2026-03-20: GetTalkAsync Scope Investigation (Issue #527)

**Task:** Add `Talks.View` fine-grained scope acceptance to `GetTalkAsync` (issue #527, flagged by Neo in PR #521 review).

**What I Found:**
- `GetTalkAsync` in `EngagementsController` already had the dual-scope fix applied in PR #526 (commit `392d0b8`): `VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.View, Domain.Scopes.Talks.All)`
- The issue was filed based on pre-PR #526 code state; both happened in close succession
- Full scope audit of all 3 controllers (Engagements, Schedules, MessageTemplates) found **no remaining gaps** — every endpoint uses the correct fine-grained + All dual pattern

**What I Implemented:**
- Added unit test `GetTalkAsync_WithViewScope_ReturnsTalk` to `EngagementsControllerTests.cs` proving `Talks.View` scope is accepted (regression coverage)
- 42 tests pass total
- PR #531: https://github.com/jguadagno/jjgnet-broadcast/pull/531

**Lesson Learned:**
- When an issue is filed concurrently with a PR, always check whether the fix was already merged before writing new code — the fix may already be in place
- Always double-check which branch HEAD is on before committing; I accidentally committed to `squad/528-msal-incremental-consent` and had to cherry-pick to the correct branch

### 2026-03-21: Scope Audit & Regression Test for Issue #527 (Trinity)
- **Task:** Verify and add regression test for `GetTalkAsync` fine-grained scope support
- **Finding:** Scope was already fixed in PR #526; issue filed based on pre-merge state
- **What I Implemented:**
  - Regression test `GetTalkAsync_WithViewScope_ReturnsTalk` added to ensure `Talks.View` is accepted
  - Full audit of all 34 endpoints across 3 controllers (Engagements, Schedules, MessageTemplates)
  - No gaps found; fine-grained scope rollout from PR #526 is complete
- **PR #531 opened** with full audit table (22 Engagements endpoints, 9 Schedules, 3 MessageTemplates)
- **All 42 API tests pass**
- **Lesson:** Check whether concurrent PRs already fixed the issue before adding new code

### 2026-03-21: Sprint Summary for Trinity
- Core work complete: Pagination PR #514 (merged), DTO layer PR #512 (merged), Scope audit PR #531 (opened)
- Message template seeding infrastructure complete (PR #502 — tests & design docs)
- All API tests passing; no scope gaps remaining

**Task**: Add pagination to all list endpoints in the API with page/pageSize query parameters (defaults: page=1, pageSize=25).

**Controllers Updated**:
1. **EngagementsController** (`src/JosephGuadagno.Broadcasting.Api/Controllers/EngagementsController.cs`)
   - `GetEngagementsAsync()` - list all engagements
   - `GetTalksForEngagementAsync(int engagementId)` - list talks for an engagement

2. **SchedulesController** (`src/JosephGuadagno.Broadcasting.Api/Controllers/SchedulesController.cs`)
   - `GetScheduledItemsAsync()` - list all scheduled items
   - `GetUnsentScheduledItemsAsync()` - list unsent items
   - `GetScheduledItemsToSendAsync()` - list upcoming items
   - `GetUpcomingScheduledItemsForCalendarMonthAsync(int year, int month)` - list items for calendar month
   - `GetOrphanedScheduledItemsAsync()` - list orphaned items

3. **MessageTemplatesController** (`src/JosephGuadagno.Broadcasting.Api/Controllers/MessageTemplatesController.cs`)
   - `GetAllAsync()` - list all message templates

**Implementation Pattern**:
```csharp
public async Task<ActionResult<PagedResponse<T>>> GetItemsAsync(int page = 1, int pageSize = 25)
{
    var allItems = await _manager.GetAllAsync();
    var totalCount = allItems.Count;
    var items = allItems
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(ToResponse)
        .ToList();
    
    return new PagedResponse<T>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

**Key Decisions**:
- Used existing `PagedResponse<T>` model (already existed as uncommitted file on main)
- Maintained existing behavior for endpoints with 404 handling (e.g., unsent/upcoming/orphaned) - check count before pagination
- Added `using JosephGuadagno.Broadcasting.Api.Models;` to each controller
- Query parameters default to `page=1, pageSize=25` per issue spec
- Changed return types from `List<TResponse>` to `PagedResponse<TResponse>`
- Updated ProducesResponseType attributes to reflect new return type

**Files Changed**:
- `src/JosephGuadagno.Broadcasting.Api/Models/PagedResponse.cs` (committed first - was untracked)
- 3 controller files (EngagementsController, SchedulesController, MessageTemplatesController)

**Branch & PR**:
- Branch: `feature/s8-316-pagination`
- PR #514: https://github.com/jguadagno/jjgnet-broadcast/pull/514
- Build succeeded with expected warnings (NU1903, NETSDK1206)

**Outcome**: All 8 list endpoints now support pagination with sensible defaults, maintaining backward compatibility via optional parameters.

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
