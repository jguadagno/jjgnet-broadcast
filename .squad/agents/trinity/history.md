# Trinity - History

## Summary

Trinity (Backend API Developer) implements core API functionality including CRUD endpoints, authentication/authorization workflows, OAuth token refresh, and data persistence. Work spans three layers: Controllers (HTTP routing), Managers (business logic), and Data/Data.Sql (Entity Framework Core persistence). Key contributions include EngagementSocialMediaPlatforms CRUD endpoints, UserApprovalManager for RBAC workflows, OAuth token refresh with token versioning, ownership isolation enforcement, and `IMemoryCache` caching layer for managers. Trinity follows Neo's architectural patterns: explicit service contracts with DTOs, response mapping to isolate Data layer changes from API contracts, and role-based authorization. Established pattern: implement feature vertically from API controller through Manager to Data layer, write explicit request/response types in API, and map Data objects to DTOs before returning. Close collaboration with Tank (integration tests), Switch (Web-layer service mapping), and Neo (architectural reviews). Notable: Trinity maintains API contract stability by mapping internal changes to stable response shapes, preventing breaking changes to Web-layer consumers. Key decision: use DTOs consistently for all API responses to maintain contracts.

### 2026-05-12 — Team: Decisions Merged to Central Log

**Status:** ✅ COMPLETE — 9 decisions merged from inbox to `.squad/decisions.md`

Trinity's decisions recorded:
- **2026-05-11:** FeedCheck EntraOId — Empty String for System Collectors (source-to-item taxonomy established)
- **2026-05-11:** Rename SyndicationFeedSource → SyndicationFeedItem, YouTubeSource → YouTubeItem (135 files, ✅ tests passing)
- **2026-05-13:** UserCollectorYouTubeChannel ApiKey → Azure Key Vault (implement)

Trinity's work on `issue-950-sanity-check` is now formally recorded. Awaiting fix for 4 blocking issues identified in Neo's review (B1-B4) before merge.

---

### 2026-05-14 — Neo Review Fixes: issue-950-sanity-check

**Status:** ✅ COMPLETE — commit 6a56416; 157 Functions tests passing, build clean

Fixed all 4 blocking issues and 3 warnings from Neo's review:
- **B1:** Added missing `namespace JosephGuadagno.Broadcasting.Managers;` to both manager files
- **B2:** Added `IYouTubeReader.GetAsync(ownerOid, sinceWhen, IYouTubeSettings)` per-user overload; refactored `YouTubeReader` to share `GetItemsAsync` private helper; wired `LoadNewVideos` to resolve Key Vault API key per channel
- **B3:** Registered `IUserCollectorSpeakingEngagementDataStore` + `IUserCollectorScheduledItemDataStore` in `AddSqlDataStores()`
- **W1:** Registered `IUserCollectorScheduledItemDataStore` + manager in API `Program.cs`
- **W2:** Removed duplicate `IYouTubeItemDataStore`/`IYouTubeItemManager` block from API `Program.cs`
- **W3:** Created `UserCollectorScheduledItemRequest/Response` DTOs + AutoMapper mappings

**Learnings:**
- When an interface changes signature, all mocks in tests using that interface must be updated immediately — the build won't catch mock signature mismatches, only test failures will.
- `GetApiKeyAsync` requires a mock in any test that exercises a code path using `IUserCollectorYouTubeChannelManager`; Moq returns `null` by default for reference types, which can silently skip logic.

---

### 2026-05-XX — Issue #936: Add IMemoryCache caching to MessageTemplateManager

**Status:** ✅ COMPLETE — PR #940 on `issue-936-messagetemplates-caching`; 253 tests passing

**What was delivered:**
- `IMessageTemplateManager` (Domain/Interfaces): interface with `GetAsync`, `GetAllAsync` (admin + owner-filtered overloads with filter/sort/page), and `UpdateAsync`
- `MessageTemplateManager` (Managers): `IMemoryCache` implementation
  - Full list cached at `MessageTemplate_All` with 5-min absolute expiry
  - Individual items cached at `MessageTemplate_{platformId}_{messageType}`
  - `ApplyFilterSortPage` private helper handles in-memory filter/sort/pagination for both admin and owner-filtered paths
  - `InvalidateListCaches()` + individual key removal on `UpdateAsync`
- `MessageTemplatesController`: swapped `IMessageTemplateDataStore` → `IMessageTemplateManager` (4 call sites)
- `Program.cs`: added `services.TryAddScoped<IMessageTemplateManager, MessageTemplateManager>()` after the DataStore registration
- `MessageTemplatesControllerTests`: mocks `IMessageTemplateManager` instead of `IMessageTemplateDataStore`; admin and owner-filtered path tests unchanged in intent

**Key patterns confirmed:**
1. When a controller was calling `IDataStore` directly with no Manager, create `IManager` + `Manager` from scratch following `SocialMediaPlatformManager` as the gold standard.
2. `ApplyFilterSortPage` static helper: pull all items from cache once, then filter/sort/page in-memory. Avoids separate cache keys per query combination.
3. `InvalidateListCaches()` on any mutation is sufficient when there is no Add/Delete — only `UpdateAsync` exists.
4. Git branch confusion guard: ALWAYS run `git branch --show-current` before any `git add` or `git commit`. Multiple branch switches in a session can leave HEAD on an unexpected branch.

---


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

### 2026-05-12 — Issue #950: Full CSRF Token Validation Sweep

1. CodeQL `cs/web/missing-token-validation` alerts can be stale: by the time the sweep ran, all 8 API controllers already had `[IgnoreAntiforgeryToken]` at the class level and all Web `[HttpPost]` methods already had `[ValidateAntiForgeryToken]`. Always read the actual file before applying a mechanical fix.
2. The correct audit order is: (a) read the file, (b) check for the attribute, (c) only edit if truly missing. A grep-only approach without reading the file can produce false negatives when attribute names are split across lines or preceded by unusual whitespace.
3. Additional Web controllers (LinkedInController, HomeController, HelpController, AccountController) had zero `[HttpPost]` methods — they are GET-only and do not require `[ValidateAntiForgeryToken]`.

### 2026-05-11 — Issue #950: EntraOId user separation for FeedCheck C# layer

1. When a parallel agent (Morpheus) deletes a shared utility file (e.g., `CollectorOwnerOidResolver.cs`) while doing SQL-only work, restore it immediately — the deletion cascades into CS0103 build errors across every caller.
2. The `GetByNameAsync` composite-key pattern (Name + EntraOId) is the correct approach for any table gaining per-user row-level isolation; update the interface, data store, manager, and ALL callers atomically in one PR.
3. Test files prepared by a parallel agent may be missing `using System.Threading;` when they reference `CancellationToken` in Moq `Setup` lambdas — always check the build before committing.
4. In LoadNewPosts and LoadNewVideos, the OID resolver MUST run before `GetByNameAsync` so the correct composite key (Name + ownerOid) is available for the feed-check lookup. Reorder accordingly.

### 2026-05-11 — Issue #950 (sanity-check): SyndicationFeedSource→SyndicationFeedItem, YouTubeSource→YouTubeItem rename

1. Use `git mv` for all file renames to preserve history; run content-replacement AFTER all renames are complete so new filenames are visible to `Get-ChildItem`.
2. Order PowerShell `-replace` chains longest-specific first (e.g., `ISyndicationFeedSourceDataStore` before `SyndicationFeedSource`) so compound names resolve correctly and aren't double-replaced.
3. The `feedSource` variable in `SyndicationFeedReader.cs` refers to the .NET BCL `FeedSource` (Atom/RSS) object — it is a different concept from the domain `SyndicationFeedSource` and must NOT be renamed.
4. After a global rename, always verify exceptions (`SourceSystems`, `SourceTag`, `SyndicationFeedReader`, `UserCollector*`) with a targeted `Select-String` pass before building.
5. SQL table renames need both a migration script (`sp_rename`) for existing environments AND updated `table-create.sql`/`data-seed.sql` for fresh environment bootstraps.


### 2026-05-12 — Issue #950 (sanity-check): CollectorSettings page refactor

1. When removing inline POST actions from a settings controller (in favour of dedicated CRUD controllers), audit the `using` directives immediately — domain model types (`UserCollectorFeedSource`, `UserCollectorYouTubeChannel`) and utilities (`LogSanitizer`) referenced only by the removed methods become dead imports and should be removed to keep the file clean.
2. `BuildPageViewModelAsync` is the single source of truth for populating the page view model; adding a new collector type only requires: fetch via service (admin/non-admin branch), inline projection to ViewModel, assign to the new collection property.
3. The inline projection pattern (`.Select(x => new ViewModel { ... }).ToList()`) is the established pattern in this controller — do not introduce AutoMapper for new collections added to `BuildPageViewModelAsync` unless the rest of the method already uses it.

### 2026-05-13 — Issue #954: UserCollectorYouTubeChannel ApiKey → Key Vault

1. When removing a domain-model property that the Web layer uses as a "pass-through" to the API (the domain model is serialized and POSTed to the API endpoint), keep it on the domain model as a **transient field** alongside the new persisted field (`ApiKeySecretName`). The EF model should only have the persisted column; the mapping profile ignores the transient field in both directions.
2. Use `ReverseMap()` with a leading `.ForMember(dest => dest.TransientProp, o => o.Ignore())` for EF↔Domain maps when the domain model has transient-only properties that the EF model doesn't have. The reverse direction (Domain→EF) auto-ignores unmapped source properties.
3. `WebMappingProfile` must ignore `ApiKey` in Domain→ViewModel (never pre-populate a password field) and `ApiKeySecretName` in ViewModel→Domain (controller handles KV; the ViewModel's `ApiKey` flows via the transient domain property).
4. The `BroadcastingProfile` and `UserCollectorMappingProfile` in `Data.Sql` are BOTH registering the same `EF ↔ Domain` YouTube channel map. Both must be updated when the domain model changes, or the second registration will silently override the first and cause `AssertConfigurationIsValid` failures.
5. The Key Vault secret-name pattern used by `UserPublisherSettingManager` (`youtube-channel-apikey-{sanitizedOwnerOid}-{sanitizedChannelId}` with `[^a-zA-Z0-9\-]→-`) must be reproduced verbatim so ops can find secrets by naming convention across all entities.
6. When converting a primary-constructor manager class to a standard constructor (to inject `IKeyVault` + `ILogger`), explicitly declare all private readonly fields and wire them from the constructor. The DI container resolves the three dependencies automatically from `TryAddScoped` — no `Program.cs` changes needed.

