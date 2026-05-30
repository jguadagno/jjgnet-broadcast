# History

> Learnings before 2026-04-25 archived to history-archive.md (2026-05-25)
> Earlier learnings (2026-05-16 to 2026-05-25) archived to history-summary.md

## Issue #995 Phase 2 & 3 — 2026-05-26 to 2026-05-29

**Status:** ✅ FULLY COMPLETE (see history-summary.md for detailed learnings)

**Scope:** Collector dispatch routing (Task 1–3), Event Grid removal, NextRunDateUtc efficiency, Phase 3 API CRUD + Web UI, PR #998 opened with cleanup

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

## NextRunDateUtc Efficiency & Phase 3 API/Web (2026-05-26–2026-05-26)

**Status:** ✅ COMPLETE (see history-summary.md for technical details)

---

## Learnings

- 2026-05-28: Web `IDownstreamApi` service wrappers should inject `ILogger<TService>` and log `GetForUserAsync`/`PostForUserAsync`/`PutForUserAsync` nulls plus delete calls that return anything other than `204 NoContent`; only `GetOptionalForUserAsync` nulls stay silent, and any logged string identifiers (owner OIDs, platform names, event types, handles) must be wrapped with `LogSanitizer.Sanitize()`.
- 2026-05-29: Dispatcher → Distributor / Platforms full rename sweep (82 files, commit `63d6c7c6`). Key file locations: Domain models at `Domain/Models/UserEventDistributorMapping.cs`, interfaces at `Domain/Interfaces/IUserEventDistributorMapping*.cs`, EF model at `Data.Sql/Models/UserEventDistributorMapping.cs`, data store at `Data.Sql/UserEventDistributorMappingDataStore.cs`, manager at `Managers/UserEventDistributorMappingManager.cs`. Functions distributor services live in `Functions/Services/CollectorEventDistributor.cs` + `ScheduledItemEventDistributor.cs`, trigger functions in `Functions/Distributors/`. API platform controllers live in `Api/Controllers/Platforms/`, distributor mapping controller in `Api/Controllers/Distributors/`, RandomPostSettings controller moved to `Api/Controllers/Publishers/`. Web views live under `Views/Platforms/` and `Views/UserEventDistributorMapping/`. `ISocialMediaDispatcher` was intentionally left unchanged (publisher-layer abstraction). The `local.settings.json` Azure Function names were also updated by the sub-agent.

---

## 2026-05-30 — Dispatcher → Distributor Rename Completion Verification

**Status:** ✅ VERIFICATION QUEUED

- Comprehensive manual rename of "Dispatcher" → "Distributor" across 24+ files completed by Joseph Guadagno
- Trinity spawned to verify: build success, CI-aligned test suite pass, straggler scan for missed references
- Session logged to `.squad/log/2026-05-30T09-42-44-distributor-rename.md`
- All naming now consistent across domain, data, managers, API, Web, and Functions layers
- No regressions expected; straggler scan will confirm zero missed "Dispatcher" references

