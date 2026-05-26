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
- **For archived learnings:** See history-summary.md

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

