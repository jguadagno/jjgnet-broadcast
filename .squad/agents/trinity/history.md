# History

> Learnings before 2026-04-25 archived to history-archive.md (2026-05-25)
> Earlier learnings (2026-05-16 to 2026-05-25) archived to history-summary.md

## Phase 2 Task 2 — Collector Dispatch Routing (2026-05-26)

**Status:** ✅ COMPLETE — commit `41db74f6` on branch `issue-995-per-user-publisher-routing`

### What was done

- Created `ICollectorEventPublisher` / `CollectorEventPublisher` service (`Services/`) that:
  - Looks up `UserEventPublisherMapping` per user × event type via `GetByUserAndEventTypeAsync`
  - Renders message templates with `IMessageTemplateManager.GetAsync(int platformId, …)` + `IPostComposer`
  - Dispatches per-platform to storage queues using the `PlatformQueues` dict pattern from `RandomPosts.cs`
  - Creates a fresh `SocialMediaPublishRequest` per platform to avoid cross-platform mutation
- Replaced `IEventPublisher` in `LoadNewPosts`, `LoadNewVideos`, `LoadNewSpeakingEngagements` with `ICollectorEventPublisher`; each collector now calls `PublishSyndicationFeedItemAsync` / `PublishYouTubeItemAsync` / `PublishSpeakingEngagementAsync` per saved item
- Deleted 16 dead `ProcessNew*` functions (Bluesky/Twitter/LinkedIn/Facebook × Feed/YouTube/SpeakingEngagement/RandomPost)
- Registered `CollectorEventPublisher` as `AddScoped<ICollectorEventPublisher, CollectorEventPublisher>()` in `Program.cs`
- Added 10 unit tests; updated existing collector tests to use `ICollectorEventPublisher` mock

### Key technical learnings

- **`UserEventPublisherMapping.IsActive`** — property is `IsActive`, not `IsEnabled`. `GetByUserAndEventTypeAsync` filters for active-only.
- **`IMessageTemplateManager.GetAsync` int overload** — use `GetAsync(int platformId, string messageType, string ownerOid, CancellationToken)`, since `SocialMediaPlatformId` is already an int.
- **Sequential foreach** — team rule: no `Task.WhenAll` on shared scoped `BroadcastingContext` operations.
- **`QueueClient.SendMessageAsync` Moq verify** — assert via `_queueClient.Invocations.Any(i => i.Method.Name == nameof(QueueClient.SendMessageAsync))` to avoid Moq overload ambiguity.
- **Existing tests needed updating:** `LoadNewPostsTests`, `LoadNewVideosTests`, `LoadNewSpeakingEngagementsTests` all referenced `IEventPublisher` in constructors and assertions.

---

## Phase 2 Task 3 — ScheduledItems Event Grid → Per-User Routing (2026-05-26)

**Status:** ✅ COMPLETE — commit `0d071bbe` on branch `issue-995-per-user-publisher-routing`

### What was done

- Created `IScheduledItemEventPublisher` / `ScheduledItemEventPublisher` service that:
  - Takes a due `ScheduledItem` and queries `UserEventPublisherMapping` for the owner
  - Renders message via `IMessageTemplateManager` + `IPostComposer`
  - Dispatches to all active target platforms' queues
  - Creates fresh `SocialMediaPublishRequest` per platform
- Replaced `IEventPublisher` in `Publishers\ScheduledItems.cs` with `IScheduledItemEventPublisher.PublishAsync`
- Deleted 4 dead `ProcessScheduledItemFired` Event Grid subscriber functions
- No schema changes — `ScheduledItem` event type already in `2026-05-26-per-user-publisher-routing-tables.sql`
- Added 9 unit tests validating per-user routing logic
- Deregistered `IEventPublisher` from Functions host; Event Grid simulator topic removed

### Key technical learnings

- **Event Grid Topic cleanup** — When deleting Event Grid subscriber functions, also remove the corresponding topic/simulator definition from Aspire AppHost to prevent orphaned topics and unused container resources.
- **ScheduledItem context complete** — `ScheduledItem` model now carries owner, event type, target platforms, and message composition in the same sequential DbContext-safe flow as `ICollectorEventPublisher`. The `Publishers\ScheduledItems.cs` timer focuses on orchestration (what's due?) and sent-flag updates, while `ScheduledItemEventPublisher` handles all dispatch.
- **Phase 2 unified:** All event dispatch (collectors + scheduled) is now direct per-user routing. No Event Grid bridge layer remains.

---

## Phase 2 Complete (All 3 Tasks)

**Status:** ✅ FULLY COMPLETE

- Task 1: Collector dispatch routing ✅
- Task 2: Cron-scheduled item routing ✅
- Task 3: ScheduledItems Event Grid → per-user ✅

**Next:** Phase 3 — Random Posts per-user scheduling and routing.

---

## Key Learnings (Recent)

- **AutoMapper `ReverseMap()` with EF tracked entities:** Never use `ReverseMap()` on collections without explicit `Ignore()` — AutoMapper will replace tracked EF collections with untracked objects, causing "Unexpected entry.EntityState: Detached" errors.
- **Null guards matter:** When removing dead code, preserve unrelated guards (e.g., `newItems == null ||` in collector loops) — removal causes `NullReferenceException` when readers return `null`.
- **EF DbContext not thread-safe:** Never use `Task.WhenAll` when managers share a single scoped DbContext. Use sequential awaits instead.
- **Event Grid cleanup:** Deregister Event Grid services from host and remove simulator topic definitions when all subscribers are deleted.
- **2026-05-26T11:17:08.070-07:00 — Per-user settings API CRUD pattern:**
  New per-user publisher settings endpoints fit the existing
  `Publishers/...` route family, use class-level `[Authorize]` +
  `[IgnoreAntiforgeryToken]`, enforce `CreatedByEntraOid` ownership on item
  routes, stamp owner OIDs from claims on create, and use separate
  create/update DTOs so PUT can preserve omitted optional fields via
  conditional AutoMapper mapping.
- **2026-05-26T11:17:08.070-07:00 — Per-user settings Web UI pattern:**
  Web controllers for per-user publisher settings should stay behind
  `I...Service` HTTP wrappers, populate platform dropdowns through
  `ISocialMediaPlatformService`, centralize event-type labels/icons in a shared
  constant, and round-trip editable `DateTimeOffset` values through a
  `datetime-local` input plus hidden UTC field so the browser shows local time
  while the API still receives UTC.
- **For archived learnings:** See history-summary.md

---

## NextRunDateUtc Efficiency Fix (RandomPosts) — 2026-05-26

**Status:** ✅ COMPLETE — branch `issue-995-per-user-publisher-routing`

### What was done

- Added `NextRunDateUtc DATETIMEOFFSET NULL` column to `UserRandomPostSettings` in SQL (`table-create.sql` + idempotent migration `2026-05-26-userrandomposts-add-nextrundate.sql`)
- Added filtered index `IX_UserRandomPostSettings_IsActive_NextRunDateUtc WHERE [IsActive] = 1`
- Added `NextRunDateUtc` property to both domain and EF models
- Added `GetAllDueAsync(DateTimeOffset utcNow, …)` and `UpdateNextRunAsync(int id, DateTimeOffset? nextRun, …)` to `IUserRandomPostSettingsDataStore`, `IUserRandomPostSettingsManager`, and their implementations
- Rewrote `RandomPosts.cs` to: inject `IUserRandomPostSettingsManager`, call `GetAllDueAsync` (SQL does the due-check, no more cron parsing per-row just to filter), flat `foreach`, always advance `NextRunDateUtc` after each attempt (dispatch success, no feed item, no template, compose failure), skip `UpdateNextRunAsync` only for invalid cron expressions
- Updated `RandomPostsTests.cs` to mock `IUserRandomPostSettingsManager` instead of the data store, and assert that `UpdateNextRunAsync` is called once per dispatch (including no-feed-item path)
- Added 8 new `UserRandomPostSettingsDataStoreTests` covering `GetAllDueAsync` and `UpdateNextRunAsync` (null, past, future, inactive, unknown-id)
- All 286 tests pass

### Key technical learnings

- **SQL-first filtering:** Moving cron scheduling to a `WHERE NextRunDateUtc IS NULL OR NextRunDateUtc <= @utcNow` query eliminates O(n × cron-parse) work in the function host; cron parsing remains only for computing the *next* occurrence after dispatch.
- **Always advance on attempt:** Advancing `NextRunDateUtc` regardless of dispatch outcome (except invalid cron) prevents an infinite retry storm for recoverable failures (no feed item, no template). Invalid cron cannot compute next occurrence, so those rows are skipped entirely.
- **`AdvanceNextRunAsync` overloads:** One overload takes a pre-parsed `CronExpression` (main dispatch path, already parsed). A second takes `DateTimeOffset utcNow` and re-parses internally (early-exit paths like unknown platform). This avoids re-parsing on the happy path while still being safe on edge paths.
- **`CronExpression.GetNextOccurrence` returns `DateTimeOffset?`:** Null for impossible expressions (e.g., Feb 31). Passing null to `UpdateNextRunAsync` clears the field, which re-includes the row on the next invocation — handled gracefully.
- **No AutoMapper profile changes needed:** `NextRunDateUtc` is a same-name same-type scalar; AutoMapper maps it automatically through the existing `ReverseMap()` profile.


---

## Phase 3 Part 1 — API CRUD Endpoints (2026-05-26T18:17:08Z)

**Status:** ✅ COMPLETE — commit `ca59c43b` on branch `issue-995-per-user-publisher-routing`

### What was done

- Created `RandomPostSettingsController` with GET, POST, PUT, DELETE under `/api/Publishers/RandomPostSettings`
  - Class-level `[Authorize]` and `[IgnoreAntiforgeryToken]`
  - Stamp `CreatedByEntraOid` from authenticated user claims on create
  - Owner-based access checks on item routes (get-by-id, update, delete)
  - Recalculate onboarding after mutations
- Created `EventPublisherMappingController` with GET, POST, PUT, DELETE under `/api/Publishers/EventPublisherMappings`
  - Same auth + ownership pattern as RandomPostSettings
  - Recalculate onboarding after mutations
- Created separate Create/Update DTOs for both controllers to preserve omitted optional fields during PUT
- Added AutoMapper profiles in `Data.Sql/MappingProfiles` and `Api/MappingProfiles` for DTO mappings
- Added comprehensive xUnit unit tests (both controllers) with:
  - Happy path CRUD operations
  - Ownership checks (IDOR prevention)
  - Authorization enforcement
  - Recalculate onboarding side effects
- All tests passing, build passing

### Next
- Phase 3 Part 2: Web app integration (consume the new API endpoints)

---

## Phase 3 Part 2 — Web UI for per-user publisher settings (2026-05-26T18:55:48Z)

**Status:** ✅ COMPLETE — commit `e86fc661` on branch `issue-995-per-user-publisher-routing`

### What was done

- Created `UserRandomPostSettingsController` with Index/Create/Edit/Delete under `/Publishers/UserRandomPostSettings`
  - Consumes API via `IUserRandomPostSettingsService` HTTP wrapper
  - Ownership-based access control via Web middleware session context
  - Datetime-local input pattern with hidden UTC field
- Created `UserEventPublisherMappingController` with Index/Create/Edit/Delete under `/Publishers/UserEventPublisherMapping`
  - Same HTTP wrapper + ownership pattern as UserRandomPostSettings
  - Multi-select platform picker with icon rendering
- Created `IUserRandomPostSettingsService` / `IUserEventPublisherMappingService` for API integration
- Created `PublisherEventTypes.cs` constants file centralizing event-type labels and collector icons
- Created Razor views (Index, Create, Edit, Delete) for both controllers with:
  - `datetime-local` input fields + hidden UTC binding
  - Platform/event-type dropdowns via metadata services
  - Validation summaries and error handling
- DI registration for controllers and services in `Program.cs`
- Navigation link integration for Web sidebar menu
- All tests passing, full build passing

### Key technical patterns

- **Web → API:** Controllers consume downstream API wrappers (services) rather than injecting managers. This matches existing Web architecture (Twitter/YouTube/etc. controllers).
- **Datetime-local pattern:** For editable `DateTimeOffset` fields, use `<input type="datetime-local" />` visible to user + hidden UTC field populated by browser script. UI stays local-friendly, API contract remains UTC.
- **Centralized event-type metadata:** Avoid duplicating event-type labels/icons across views. Use `PublisherEventTypes.cs` constant class.
- **Platform metadata:** `ISocialMediaPlatformService` resolves platform names and icons; centralize filtering/sorting in service, not in views.

### Phase 3 Complete

All phases of issue #995 are now fully complete:
- Phase 1: Database schema ✅
- Phase 2: Collector/ScheduledItems event dispatch → per-user routing ✅
- Phase 3 Part 1: API CRUD endpoints ✅
- Phase 3 Part 2: Web UI ✅

**Next:** Code review, merge to main, close issue #995.

---

## PR Opened — Issue #995 Per-User Publisher Routing (2026-05-26T11:56:31.095-07:00)

- Cleanup commit `83e4a8a5` removed dead global `RandomPostSettings`, `IEventPublisher`, Event Grid topic configuration, and simulator/test scaffolding after the per-user routing migration.
- Manual production steps issue opened: #997 (`squad:Joe`).
- Pull request opened: #998 — `feat(#995): per-user publisher routing — replace Event Grid dispatch`.
- Validation before push: `dotnet build .\src\ --no-restore --configuration Release` ✅ and `dotnet test .\src\ --no-build --verbosity normal --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader"` ✅.

