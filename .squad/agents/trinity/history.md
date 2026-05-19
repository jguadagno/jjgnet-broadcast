## Summary (archived 2026-05-19)

Older history entries have been archived. See history-archive.md for complete session logs.

---
2. **No defensive fallback in data stores** — Without a try/catch, the `SqlException` propagated through the manager to `PublishersController.GetAllAsync`, which uses `Task.WhenAll` for all 4 platforms. One failure caused the entire aggregate to fail and the page to error. Fixed in commit `3ef227a2`: try/catch added to all 4 operations (`GetByUserAsync`, `GetByIdAsync`, `SaveAsync`, `DeleteAsync`) across all 4 data stores.

**Schema analysis:**
- EF entity model: 12 properties (including `CreatedOn`, `LastUpdatedOn`) ✓
- Domain model: 12 properties ✓
- Migration SQL: 12 columns ✓
- `table-create.sql` (post-fix): 12 columns ✓
- `BroadcastingContext` Fluent API: all 12 mapped, `CreatedOn`/`LastUpdatedOn` with `HasDefaultValueSql("(getutcdate())")` ✓
- **No schema mismatch existed** between EF model and DB.

**Anomaly note — missing `CreatedOn` in the error query:**
The failing SQL shown in the task description omits `[u].[CreatedOn]` (11 columns instead of 12). The current code generates 12-column SELECT. This indicates the error was captured against an older compiled binary. The actual SQL Server error would have been "Invalid object name 'UserPublisherFacebookSettings'" — EF logs the attempted SQL, not the SQL Server error text.

**No migration script needed** — the tables were already created by the migration; `table-create.sql` is only for fresh environments. Adding them idempotently (with `IF NOT EXISTS`) to `table-create.sql` was the correct fix.

**Pattern reinforced:** Whenever adding tables via a migration, also backfill `scripts/database/table-create.sql` with an `IF NOT EXISTS` guard so Aspire fresh-environment bootstrapping stays in sync.

---

### 2026-05-16 — Concurrent DbContext Fix: Task.WhenAll → Sequential Awaits

**Status:** ✅ COMPLETE — commit 20fc6b79; 247 tests passed (0 errors, 0 failures)

**Root cause:**
`PublishersController.GetAllAsync`, `CollectorsController.GetAllAsync`, and `SchedulesController.Index` all used `Task.WhenAll` to fan out calls to managers that share a single scoped `BroadcastingContext`. EF Core's `DbContext` is not thread-safe for concurrent operations — simultaneous queries on the same context caused the SQL connection to enter a closed/corrupt state:
> "BeginExecuteReader requires an open and available Connection. The connection's current state is closed."

**Fix:** Replaced all three `Task.WhenAll` fan-outs with sequential `await` calls. The DbContext is scoped per HTTP request, so each await completes before the next begins — no concurrent access.

**Files changed:**
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\PublishersController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Collectors\CollectorsController.cs`
- `src\JosephGuadagno.Broadcasting.Web\Controllers\SchedulesController.cs`

**Learning:** Never use `Task.WhenAll` (or fire-and-forget task creation before the first await) when the underlying managers share a single scoped `DbContext`. The pattern `var t1 = Foo(); var t2 = Bar(); await Task.WhenAll(t1, t2)` looks like a safe optimization but is a correctness bug with EF Core scoped contexts. Use sequential awaits instead.

### 2026-05-19 — Fix: MSAL L1 Cache Pin Scoped to Release Builds Only

**Status:** ✅ COMPLETE — commit c5242189; Release and Debug builds both pass (0 errors, 0 warnings)

**Root cause:**
`MsalDistributedTokenCacheAdapterOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)` was set unconditionally in `Web/Program.cs`. While intended to pin the L1 (in-memory) cache and prevent per-request SQL reads, it propagated to the SQL (L2) distributed cache, overriding its 14-day sliding expiration. Result: forced re-login after 15 minutes of inactivity and on every app restart in development.

**Fix:**
Wrapped `options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);` in `#if !DEBUG` / `#endif`. In Debug builds, the setting is omitted → SQL cache keeps 14-day sliding expiry. In Release, the L1 pin remains → production performance optimization preserved.

**Learning:**
`MsalDistributedTokenCacheAdapterOptions` applies to **both** L1 (in-memory) and L2 (distributed/SQL) cache layers. Properties set here are not L1-exclusive — `AbsoluteExpirationRelativeToNow` silently overrides the distributed cache TTL configured at the SQL store level.

---

### GetForUserAsync<T> — 404 handling pattern (2026-05-17)

Any Web service that calls `IDownstreamApi.GetForUserAsync<T>` for a **single nullable object** (not a collection) MUST wrap the call in `catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)` and return `null`. The API legitimately returns 404 for first-time users who have no configuration yet; without the catch the exception propagates and crashes the page. The controller already handles `null` gracefully. Log the 404 as `LogInformation` (not `LogWarning`) — it is expected, not an error. Always sanitize the OID via `LogSanitizer.Sanitize(ownerOid)`.
