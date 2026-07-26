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
- 2026-05-30: Verification after Joseph's manual Dispatcher → Distributor sweep found 35 remaining code-level stragglers, all non-breaking naming leftovers in local symbols/view-model names (`collectorEventDispatcher`, `scheduledItemEventDispatcher`, `DispatcherPlatformCardViewModel`, `userEventDispatcherMappingTable`). `ISocialMediaDispatcher` references and the SQL migration rename script are intentional and should remain. Release build and CI-aligned tests still passed (`dotnet build .\src\ --no-restore --configuration Release`, `dotnet test .\src\ --no-build --verbosity normal --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader"`).

---

## 2026-06-05 — Full Table-Based Index UI Consistency Sweep

**Status:** ✅ COMPLETE

**Scope:** Standardized all 12 table-based Index views across the Web project, added `ToggleActiveAsync` to 5 service interfaces/implementations, added `ToggleActive` POST actions to 5 controllers.

### Learnings

- **ToggleActive controller insertion bug:** When appending a new method to a controller whose Delete action has a one-branch `if (result)` with no fallthrough return, the ToggleActive was accidentally inserted *inside* the `if` block, placing it outside the class scope. Always verify the Delete method closes properly (`TempData["ErrorMessage"]` + `return RedirectToAction` after the `if`) before appending.
- **Private helper method preservation:** When adding `ToggleActive` to `UserEventDistributorMappingController`, the `MapToDomainModel` private method was accidentally displaced. Always read the full tail of a controller before editing the closing region.
- **SortIcon function standard:** All Index views must use `bi-sort-alpha-up`/`bi-sort-alpha-down` (not `bi-arrow-down`/`bi-arrow-up` or `bi-sort-down`/`bi-sort-up`). Returning empty string `""` (not a default icon) when the column is not the current sort column is correct.
- **Toggle button convention:** Active items use `bi-toggle-off` (click to deactivate), inactive items use `bi-toggle-on` (click to activate). Previous SocialMediaPlatforms view had this backwards. Button class is always `btn-warning` regardless of state.
- **Add New button placement:** Two instances needed — one in the `d-flex` header div (visible at page top), one after `<partial name="_PaginationPartial" />` at page bottom. Both must be inside a role check.
- **Pagination partial for newer views:** `UserEventDistributorMapping` and `UserRandomPostSettings` Index views were missing `<partial name="_PaginationPartial" />` entirely — always add it after the table's closing `</div>`.

---

## 2026-06-05 — UI Standard Decision & Build/Test Verification

**Status:** ✅ COMPLETE — Merged to decisions.md

**Standards established:**
- Table classes: `table table-striped table-hover table-bordered`
- Sort icons: `bi-sort-alpha-down` (asc), `bi-sort-alpha-up` (desc), `""` (unsorted)
- Active status: Icon pair (`bi-check-circle-fill` green / `bi-x-circle-fill` red), no badges
- Buttons: View/Edit `btn-outline-primary`, Toggle `btn-warning`, Delete `btn-danger`
- Toggle convention: Active → `bi-toggle-off`, Inactive → `bi-toggle-on`
- Layout: Header flex div + post-pagination Add New button (both required inside role check)

**Build/Test results:**
- Release build: ✅
- Tests (CI-aligned, excluding SyndicationFeedReader): ✅ 239 passed, 0 failed
- No regressions


- Comprehensive manual rename of "Dispatcher" → "Distributor" across 24+ files completed by Joseph Guadagno
- Trinity spawned to verify: build success, CI-aligned test suite pass, straggler scan for missed references
- Session logged to `.squad/log/2026-05-30T09-42-44-distributor-rename.md`
- All naming now consistent across domain, data, managers, API, Web, and Functions layers
- No regressions expected; straggler scan will confirm zero missed "Dispatcher" references

