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

### 2026-05-08 — Issue #933: ProcessNewSpeakingEngagementFired Azure Functions

**Status:** ✅ COMPLETE — PR #939 on `issue-933-speaking-engagement-functions`; 242 tests passing

**What was delivered:**
- `Bluesky/ProcessNewSpeakingEngagementFired.cs` — Event Grid trigger → `IBlueskyManager.ComposeMessageAsync` → `BlueskyPostMessage` output queue (truncated to 300)
- `Facebook/ProcessNewSpeakingEngagementFired.cs` — Event Grid trigger → `IFacebookManager.ComposeMessageAsync` → `FacebookPostStatus` output queue
- `LinkedIn/ProcessNewSpeakingEngagementFired.cs` — per-user OAuth via `IUserOAuthTokenManager`; null-guard on `CreatedByEntraOid` before token lookup
- `Twitter/ProcessNewSpeakingEngagementFired.cs` — Event Grid trigger → `ITwitterManager.ComposeMessageAsync` → `TwitterTweetMessage` output queue
- `ConfigurationFunctionNames.cs`: 4 new function-name constants
- `Metrics.cs`: 4 new telemetry event constants

**Key patterns confirmed:**
1. `LinkedInPostLink` (and `FacebookPostStatus`, `TwitterTweetMessage`) live in `JosephGuadagno.Broadcasting.Domain.Models.Messages`. `BlueskyPostMessage` is the exception — it's in `JosephGuadagno.Broadcasting.Managers.Bluesky.Models`.
2. `ITwitterManager` lives in the **Domain** layer (`JosephGuadagno.Broadcasting.Domain.Interfaces`), not in a `Managers.Twitter` project. Unlike every other platform manager whose interface lives in the manager project.
3. `ILinkedInManager` is in `JosephGuadagno.Broadcasting.Managers.LinkedIn.Models` (unusual — the interface file is physically in the `Models/` folder, not `Interfaces/`).
4. `engagement.CreatedByEntraOid` is `string?` (nullable). Always null-guard before calling `IUserOAuthTokenManager.GetByUserAndPlatformAsync` to avoid CS8604.
5. Per-user isolation (OAuth token lookup) is LinkedIn-only; Bluesky, Facebook, and Twitter use shared credentials.
6. The synthetic `ScheduledItem` approach: set `ItemType = ScheduledItemType.Engagements` and `ItemPrimaryKey = engagement.Id`. The platform manager internally re-fetches the engagement when rendering Scriban templates.

---

### 2026-05-08 — Issue #937 PR #941: Fix SentScheduledItemAsync cache invalidation

**Status:** ✅ COMPLETE — commit `5cf213d` on `issue-937-user-owned-caching`; all tests pass

**What was delivered:**
- `ScheduledItemManager.SentScheduledItemAsync(int, DateTimeOffset, CancellationToken)`: applied fetch-before-mutate pattern — `GetAsync` first to capture `CreatedByEntraOid`, early `return false` if item not found, `InvalidateUserCaches(entity.CreatedByEntraOid)` after a successful data-store update
- Single-arg overload delegates to the full overload unchanged, picks up fix automatically
- Comment posted on PR #941

**Key patterns confirmed:**
1. When `SentScheduledItemAsync` is a void-like mutation, still fetch entity first to get owner for cache invalidation — same as `DeleteAsync(int primaryKey)` pattern.
2. Both overloads are handled by fixing only the full overload since the single-arg delegate-chains to it.
3. Branch lives in worktree at `D:\Projects\jjgnet-broadcast-937` — always work there, never `git checkout` in main tree.

---

## Learnings
