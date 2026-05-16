# Trinity - History

## Summary

Trinity (Backend API Developer) implements core API functionality including CRUD endpoints, authentication/authorization workflows, OAuth token refresh, and data persistence. Work spans three layers: Controllers (HTTP routing), Managers (business logic), and Data/Data.Sql (Entity Framework Core persistence). Key contributions include EngagementSocialMediaPlatforms CRUD endpoints, UserApprovalManager for RBAC workflows, OAuth token refresh with token versioning, ownership isolation enforcement, and `IMemoryCache` caching layer for managers. Trinity follows Neo's architectural patterns: explicit service contracts with DTOs, response mapping to isolate Data layer changes from API contracts, and role-based authorization. Pattern: implement feature vertically from API controller through Manager to Data layer, write explicit request/response types in API, and map Data objects to DTOs before returning.

---

### 2026-05-15 — PR #963 Log Injection Fix: UserPublisherSettingService

**Status:** ✅ COMPLETE — commit eda470e7 on `issue-959-publisher-settings-phase2`

Fixed Neo's blocking `cs/log-forging` review finding: 3 `LogWarning` call sites in `UserPublisherSettingService.cs` were passing user-controlled values (`CreatedByEntraOid`, `SocialMediaPlatformName`, `platform`) directly to the logger without sanitization. Added `using JosephGuadagno.Broadcasting.Domain.Utilities;` and wrapped all user-controlled arguments with `LogSanitizer.Sanitize()`. Build clean (0 warnings, 0 errors). PR #963 unblocked.

**Learnings:**
- `LogSanitizer.Sanitize()` must be applied in the **service layer** just as strictly as in controllers.
- When a new service file is created without the `JosephGuadagno.Broadcasting.Domain.Utilities` using directive, `LogSanitizer` will not be available — always check the using is present.
- Private helper methods (e.g., `LogSaveFailure`) are equally subject to the log-injection rule — audit the full file, not just public methods.

---

### 2026-05-15 — Issue #958 Phase 1: Per-Publisher SQL Tables, EF Models, Data Stores

**Status:** ✅ COMPLETE — PR #962 on `issue-958-publisher-settings-phase1`; 28 new tests, all passing

**What was delivered:**
- SQL migration: 4 idempotent tables (Bluesky, Twitter, LinkedIn, Facebook), `UNIQUE (CreatedByEntraOid)`, `IsEnabled BIT DEFAULT 0`
- EF models, domain models, 4 `IUserPublisher*SettingsDataStore` interfaces
- `GetByUserAsync` returns `Task<T?>` (nullable single — one-to-one per user)
- `SaveAsync` upsert pattern; `LogSanitizer.Sanitize()` on all user-controlled log args
- `UserPublisherSettingsMappingProfile` — 4 `ReverseMap()` pairs
- DI: `AddSqlDataStores()`, `AddDataSqlMappingProfiles()`, `Api/Program.cs`, `Functions/Program.cs`
- Tests: 28 xUnit tests (7 per platform) using in-memory EF

**Learnings:**
- One-to-one data stores return `Task<T?>` from `GetByUserAsync`, not `Task<List<T>>`.
- `UserPublisherSettingsMappingProfile` needs no ignored fields — unlike collector channels there are no transient properties. `ReverseMap()` alone is sufficient.

---

### 2026-05-14 — Issue #950: Neo Review Fixes (issue-950-sanity-check)

**Status:** ✅ COMPLETE — commits 6a56416, 525dc2d; 157 Functions tests passing, build clean

- **B1:** Added `namespace JosephGuadagno.Broadcasting.Managers;` to both missing manager files
- **B2:** Added `IYouTubeReader.GetAsync(ownerOid, sinceWhen, IYouTubeSettings)` per-user overload; wired `LoadNewVideos` to resolve KV API key per channel
- **B3:** Registered `IUserCollectorSpeakingEngagementDataStore` + `IUserCollectorScheduledItemDataStore` in `AddSqlDataStores()`
- **W1-W3:** Fixed API DI duplicates, added `UserCollectorScheduledItemRequest/Response` DTOs + AutoMapper mappings
- XML Doc CS1573 fixes across all 8 affected API controllers (commit 525dc2d)

---

### 2026-05-08 — Issue #933/937/936/899: Core Features

**Status:** ✅ ALL COMPLETE

- **#933 PR #939** (242 tests): 4 `ProcessNewSpeakingEngagementFired` Azure Functions (Bluesky/Facebook/LinkedIn/Twitter). LinkedIn uses per-user OAuth via `IUserOAuthTokenManager`; others use shared credentials.
- **#937 commit 5cf213d**: Fixed `SentScheduledItemAsync` cache invalidation — fetch-before-mutate to capture `CreatedByEntraOid` before calling `InvalidateUserCaches()`.
- **#936 PR #940** (253 tests): `IMessageTemplateManager` + `MessageTemplateManager` with `IMemoryCache`. Two cache keys: `MessageTemplate_All` and `MessageTemplate_{platformId}_{messageType}`. `ApplyFilterSortPage` static helper for in-memory filter/sort/pagination. `InvalidateListCaches()` on `UpdateAsync`.
- **#899 PR #924** (11 tests): `ITwitterManager.ComposeMessageAsync`. `IServiceScopeFactory?` constructor overload. `ProcessScheduledItemFired` simplified from ~100 lines to single await.

---

## Learnings

### Collector Web Layer — Issue #960 Phase 2 (2026-05-16)

1. When adding route aliases to an MVC controller that uses conventional routing, switch to attribute routing with both `[Route("OldName")]` and `[Route("NewName")]` at class level. The `Index` action needs `[HttpGet("")]` and `[HttpGet("Index")]` to cover both bare path and explicit action URL.
2. Single-row-per-user Web controllers: no `id` parameter needed — actions are parameterless (or take optional `ownerOid` for admin). The API handles the upsert internally.
3. `UserCollectorScheduledItemService.SaveAsync` passes `item.CreatedByEntraOid` as `?ownerOid=` query param so the API can enforce ownership/admin checks.
4. `WebMappingProfile` ViewModel→Domain maps for single-row models need `Id`, `CreatedByEntraOid`, `CreatedOn`, and `LastUpdatedOn` all ignored — the controller sets them explicitly before calling `SaveAsync`.
5. Nullable ViewModel (`UserCollectorScheduledItemViewModel?`) is the right Index view model for single-row-per-user — renders "not configured" state without a separate sentinel value.



1. When marking old controllers `[Obsolete]`, test files that instantiate them directly will generate CS0618 build warnings. Suppress with `#pragma warning disable CS0618` at file top with a comment explaining the intent (testing backward compat during migration).
2. `IUserCollectorScheduledItemManager.GetByUserAsync` returns `Task<List<T>>` (not `Task<T?>`). For single-row-per-user patterns, call `GetByUserAsync` and take `.FirstOrDefault()` — do NOT assume a `GetSingleByUserAsync` method exists.
3. Upsert pattern for single-row controllers: call `GetByUserAsync`, if found set `config.Id = existing.Id` before calling `SaveAsync`; if not found pass `Id = 0` and `SaveAsync` creates it.
4. New `Collectors/` subfolder in `Controllers/` mirrors the existing `Publishers/` subfolder pattern. Use explicit `[Route("Collectors/YouTube/Settings")]` — not `[Route("[controller]")]` — to keep the human-readable segment independent of the class name.



1. `KeyVaultSecretOwnerType` enum uses `.ToString().ToLowerInvariant()` to produce "publisher"/"collector" — no switch, no lookup table needed.
2. `const string` fields from a static class ARE compile-time constants and can be used directly in xUnit `[InlineData]` attribute arguments.
3. Enum values (e.g., `KeyVaultSecretOwnerType.Publisher`) are also valid `[InlineData]` arguments.
4. New types go in `JosephGuadagno.Broadcasting.Domain.Utilities` namespace as sibling files alongside `KeyVaultSecretNameBuilder` and `LogSanitizer`.

### Security

1. `LogSanitizer.Sanitize()` applies in the **service layer** too — not just controllers. Private helper methods that log are equally subject.
2. Any class that logs user-controlled data must have `using JosephGuadagno.Broadcasting.Domain.Utilities;`.
3. `[IgnoreAntiforgeryToken]` sweep (2026-05-15): all 10 API controllers already compliant. Class-level is the established pattern — do not add per-method attributes.
4. CSRF sweep (2026-05-12): all Web `[HttpPost]` methods had `[ValidateAntiForgeryToken]`, all API controllers had `[IgnoreAntiforgeryToken]`. Read actual files before applying mechanical security fixes.

### Interface/Mock Patterns

1. When an interface signature changes, update all Moq mocks immediately — the build won't catch mock signature mismatches.
2. `GetApiKeyAsync` requires a mock in tests using `IUserCollectorYouTubeChannelManager`; Moq returns `null` by default.

### DTO Patterns

1. Split `XxxRequest` into `CreateXxxRequest` + `UpdateXxxRequest` when required-ness differs between Create and Update.
2. For Update: validate `!existing.HasApiKey && string.IsNullOrWhiteSpace(request.ApiKey)` in the controller after the fetch — not in ModelState.
3. AutoMapper: add `.ForMember(d => d.HasApiKey, o => o.Ignore())` for server-computed flags.
4. Razor: round-trip `bool HasApiKey` via `<input type="hidden" asp-for="HasApiKey" />` — without it, Edit POST always receives `false`.
5. `asp-for` tag helpers cannot have C# ternary expressions — use full `@if/else` blocks for conditional attributes.

### Domain Model / Mapping

1. When a domain model has transient-only properties (e.g., `ApiKey`), keep on domain model with EF mapping ignoring it. `WebMappingProfile` must ignore `ApiKey` in Domain→ViewModel.
2. `BroadcastingProfile` and `UserCollectorMappingProfile` both register the same EF↔Domain YouTube map — update both when the domain model changes.
3. One-to-one data stores use `Task<T?>` from `GetByUserAsync` (not `Task<List<T>>`).

### SQL/EF

1. `SyndicationFeedSource→FeedItem` rename (135 files): use `git mv` for renames FIRST, then content-replace. Order `-replace` chains longest-specific first.
2. SQL table renames need both `sp_rename` migration script AND updated `table-create.sql`/`data-seed.sql`.
3. CS1573 fires when XML docs have SOME `<param>` tags but missing others. Fix by adding all missing tags.
4. Always run `dotnet build .\src\` (full solution) — reveals more warnings than building a single project.

### Architecture

1. `AddSqlDataStores()` is the Web project's only SQL registration path. New data stores must be registered there.
2. Git branch confusion guard: ALWAYS run `git branch --show-current` before any `git add`/`git commit`.
3. `BuildPageViewModelAsync` is the single source of truth. Inline `.Select(x => new ViewModel { ... }).ToList()` is the established projection pattern — don't introduce AutoMapper there.
4. Per-user OAuth isolation is LinkedIn-only. Other platforms use shared credentials.
5. `IServiceScopeFactory` singleton pattern: singleton managers resolve scoped services inside async methods via `CreateScope()`. .NET DI auto-selects the longest constructor — no `Program.cs` changes needed.
6. Interface location quirks: `ITwitterManager` is in Domain layer (not Managers.Twitter); `ILinkedInManager` is physically in the `Models/` folder.

### XML Documentation

1. For `DateTimeOffset` properties, note that the value includes a UTC offset.
2. Never use "Gets or sets" as a property summary — describe the business meaning.
3. Use `<remarks>This field is required.</remarks>` not "required" in `<summary>`.
4. Class summaries that reflect an old type name (before renames) are stale — fix them.

### CollectorSettings Refactor

1. When removing inline POST actions from a settings controller, also remove now-dead `using` directives.
2. For settings-style pages: prefer redirect links to dedicated CRUD pages; reserve modals for lightweight confirmations.
3. `CollectorSettingsController` is now a **read-only page controller** — all mutations flow through dedicated controllers.

### GitHub Output Safety (Sprint 28)

1. NEVER use `gh pr create --body "..."` inline — PowerShell mangles backslashes. ALWAYS write body to file first.
2. Before all GitHub output, scan for `\word\` patterns and replace with backticks.

