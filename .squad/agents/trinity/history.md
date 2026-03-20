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
