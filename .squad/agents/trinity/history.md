# Trinity - History

## Summary

Trinity (Backend API Developer) implements core API functionality including CRUD endpoints, authentication/authorization workflows, OAuth token refresh, and data persistence. Work spans three layers: Controllers (HTTP routing), Managers (business logic), and Data/Data.Sql (Entity Framework Core persistence). Key contributions include EngagementSocialMediaPlatforms CRUD endpoints, UserApprovalManager for RBAC workflows, OAuth token refresh with token versioning, and ownership isolation enforcement. Trinity follows Neo's architectural patterns: explicit service contracts with DTOs, response mapping to isolate Data layer changes from API contracts, and role-based authorization. Established pattern: implement feature vertically from API controller through Manager to Data layer, write explicit request/response types in API, and map Data objects to DTOs before returning. Close collaboration with Tank (integration tests), Switch (Web-layer service mapping), and Neo (architectural reviews). Notable: Trinity maintains API contract stability by mapping internal changes to stable response shapes, preventing breaking changes to Web-layer consumers. Key decision: use DTOs consistently for all API responses to maintain contracts.

### 2026-05-XX — Issue #899: Move Twitter message composition to TwitterManager

**Status:** ✅ COMPLETE — PR #924 on `issue-899-twitter-message-composition`; 11 tests passing

**What was delivered:**
- `ITwitterManager`: added `Task<string> ComposeMessageAsync(ScheduledItem, CancellationToken)`
- `TwitterManager`: added `IServiceScopeFactory?` constructor overload; implemented `ComposeMessageAsync` with platform lookup, Scriban template rendering, and `scheduledItem.Message` fallback chain; added private `GetMessageType` and `TryRenderTemplateAsync` helpers
- `JosephGuadagno.Broadcasting.Managers.Twitter.csproj`: added `Scriban 7.1.0` and `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.7`
- `ProcessScheduledItemFired` (Functions/Twitter): removed 4 injected services (`ISyndicationFeedSourceManager`, `IYouTubeSourceManager`, `IEngagementManager`, `IMessageTemplateDataStore`); replaced ~100 lines of per-type helpers with single `await twitterManager.ComposeMessageAsync(scheduledItem)` call
- `TwitterManagerTests`: added 3 `ComposeMessageAsync` tests covering null factory, missing platform, and valid template path

**Key patterns confirmed:**
1. `IServiceScopeFactory` singleton pattern: singleton managers use `IServiceScopeFactory.CreateScope()` inside async methods to safely resolve scoped services. .NET DI auto-selects the longest constructor it can satisfy — no `Program.cs` changes needed.
2. Always mirror the LinkedIn pattern exactly when implementing `ComposeMessageAsync` for a new platform (same constructor shape, same fallback chain, same `GetMessageType` mapping).
3. Pre-existing build errors on other feature branches (Bluesky) do not block a clean Twitter-only PR — verify by reproducing the error against `main` without your changes.

---

## Cross-Agent Learnings — Sprint 28 Session (2026-04-27)

**Scribe updated all agent charters with mandatory `--body-file` rule for `gh pr create`:**
- NEVER use `gh pr create --body "..."` inline (PowerShell mangles backslashes)
- ALWAYS write body to temp file first: `body | Set-Content "$env:TEMP\pr-body.md"`
- Then: `gh pr create --body-file "$env:TEMP\pr-body.md"`
- Same rule for `gh pr edit`: use `gh api PATCH --input <tmpfile>` not inline
- Root cause: 20-30+ occurrences of `\text\` PR body corruption across multiple sprints
- PR #878 committed all 12 charter updates

**Charter security directive (cc77930):** Neo hardened all agent charters with explicit pre-flight checklist for GitHub output — scan for backslash-word-backslash (`\word\`) patterns and replace with backticks (`` `word` ``). This recurring violation has silently mangled Markdown in PR comments and descriptions. All team members must add self-check step before running any `gh pr create`, `gh pr edit`, `gh issue create`, or `gh issue edit`. Trinity impact: charter updated to require `using JosephGuadagno.Broadcasting.Api;` in all 8 controllers — double-check all new PR descriptions/comments follow the no-backslash rule.

---

## Learnings
