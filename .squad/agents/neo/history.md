# Neo — History

## Core Context

**Role:** Lead Reviewer & Architect  
**Specialty:** Architecture, code reviews, issue triage, sprint planning, patterns, CI/CD  
**Key Responsibilities:**
- Pull request review authority (architecture, security, patterns)
- Issue decomposition and squad assignment
- Pattern establishment and enforcement
- Sprint planning and closure

**Established patterns:**
- DTO/API: request DTOs exclude route params, return Task<ActionResult<T>>, null guard before ToResponse
- Pagination: guard divide-by-zero, clamp page≥1, pageSize 1–100
- Database: SaveChangesAsync override covers both variants
- Testing: sealed types use typed null, never Mock.Of<SealedType>()
- PR review: always verify diff against body, check issue status before review
- Authorization: GET form actions must match POST action auth level (fail-fast UX)
- Email queue: `AddMessageWithBase64EncodingAsync` (not plain `AddMessageAsync`) — Azure Functions queue triggers expect Base64
- Manager pattern: if a manager is a pure thin delegator with no logging, omit ILogger entirely to avoid CS0414 warning
- **EF Core value type defaults:** Never use `.HasDefaultValueSql()` on non-nullable value types (bool, int, DateTime) — redundant and triggers EF Core 8+ warnings
- **Health checks in ServiceDefaults:** Use conditional registration based on connection string presence — allows safe sharing across Api, Web, Functions

**Current focus:** PR #645 (Bicep IaC scaffold for #637) reviewed. REQUEST CHANGES issued — showstopper circular dependency found in module wiring.

## Summary

Lead reviewer and sprint planner. Primary domain: architecture, CI/CD, patterns, code reviews, issue triage.

**Current focus:** RBAC Phase 1 — PR #610 Round 3 review complete. APPROVED — all Round 2 findings resolved. Ready for @jguadagno merge.

**Key patterns established:**
- DTO/API: request DTOs exclude route params, return Task<ActionResult<T>>, null guard before ToResponse
- Pagination: guard divide-by-zero, clamp page≥1, pageSize 1–100
- Database: SaveChangesAsync override covers both variants
- Testing: sealed types use typed null, never Mock.Of<SealedType>()
- PR review: always verify diff against body, check issue status before review

**Backlog triage complete:** 32 issues assigned to 6 squads (neo:12, sparks:7, switch:8, trinity:2, morpheus:2, ghost:1).

**Sprint closure:** Sprint 9 (7 issues) complete via PRs #516–#526, all merged. Sprint 11 (5 issues) complete via PRs #551–#555, all merged. Three-layer auth exception defence live on main.

## Recent Work

### 2026-04-05: Infrastructure Issues Triage — #635 and #636

**Issues triaged:**
- #635: Add health checks for Api, Web applications
- #636: Setup alerting for repeated exceptions in Application Insights

**Triage outcomes:**

**Issue #635 (Health Checks):**
- **Current state:** Basic `/health` and `/alive` endpoints exist via ServiceDefaults, but only have a "self" check (always healthy)
- **Gap:** No service-specific health checks for SQL Server, Azure Storage, Key Vault, Communication Services
- **Scope:** Add AspNetCore.HealthChecks.* NuGet packages, configure dependency health checks in ServiceDefaults
- **Level of effort:** Small (2 story points, 2-3 hours)
- **Routing:** `squad:sparks` (DevOps/Infrastructure)
- **Key finding:** Infrastructure already in place — just need to add dependency-specific checks

**Issue #636 (Exception Alerting):**
- **Current state:** Comprehensive telemetry via Application Insights + OpenTelemetry + Serilog, but NO alerting configured
- **Gap:** No Azure Monitor Alert Rules, Action Groups, or Smart Detection routing
- **Scope:** Create Action Group + Alert Rules for repeated exceptions (>5 in 15 min threshold)
- **Decision point:** Bicep IaC (recommended, 3 story points) vs. Portal-only (fast-track, 1-2 hours)
- **Level of effort:** Medium (3 story points if using Bicep, ~4-6 hours)
- **Routing:** `squad:sparks` primary, `squad:neo` for Bicep review
- **Blocked on:** Joseph's decision on IaC approach and notification recipients

**Actions taken:**
- Posted comprehensive triage comments to both issues on GitHub
- Applied `squad:sparks` labels to both issues
- Identified 5 open questions for #636 requiring Joseph's input

**Learnings:**
- ServiceDefaults project is the central location for cross-cutting concerns (health checks, telemetry, observability)
- Health check endpoints automatically exclude from OpenTelemetry tracing to reduce noise
- No IaC exists for Azure infrastructure — all resources manually provisioned (opportunity for improvement)
- Serilog multi-sink pattern: Console + File + Azure Table Storage + OpenTelemetry
- GlobalExceptionHandler in Api logs all unhandled exceptions with full context

**UPDATE (2026-04-05): Issue #636 Finalized**

Joseph answered all 5 blocking questions:
1. **Notification recipients:** Email
2. **Alert threshold:** >5 exceptions in 15 minutes (Neo's recommendation accepted)
3. **Exception filtering:** Yes — exclude ValidationException, NotFoundException, and similar non-critical exceptions
4. **IaC approach:** **BOTH** — Create Bicep templates AND Portal step-by-step instructions
5. **Environments:** Production only (staging no longer exists)

**Additional decision:** Joseph wants a separate issue created for "Bicep scripts for the whole environment" — he eventually wants all Azure infrastructure as IaC.

**Actions taken:**
- Posted finalized implementation spec to issue #636 (complete, actionable)
- Updated label from `squad:sparks` to `squad:cypher` (Bicep/IaC work belongs to Cypher)
- Created **new issue #637**: "Create Bicep scripts for the entire Azure environment (Infrastructure as Code)" — epic-level initiative
- Posted triage comment on #637 with phased approach (Phase 0 = #636, then App Insights, Storage, Key Vault, SQL, App Services, Functions, etc.)
- Applied `squad:cypher` label to #637

**Key decisions recorded:**
- Alert threshold: >5 exceptions in 15 minutes (production only)
- Notification: email
- Exception filters: yes (exclude ValidationException, NotFoundException)
- IaC approach: Bicep, modular, incremental by resource type
- Environments: production only (staging decommissioned)
- Broader IaC initiative: build incrementally, issue-by-issue (not big-bang)

**New issue:** #637 — Bicep IaC for entire Azure environment (8 story points, multi-sprint epic)

**Status:** ✅ All decisions recorded and posted to GitHub. Issue #637 created and triaged. Ready for Cypher to implement #636.

---

### 2026-04-05: Email Managers — PR #623 Review (Issue #617)

**PR:** #623 — `feat: EmailSender and EmailTemplateManager for #608 email notification system`
**Branch:** `issue-617`
**Author:** Trinity
**Verdict: ⚠️ APPROVED WITH NOTES**

**What was built:** `EmailSender` (partial class + `.logger.cs`), `EmailTemplateManager`, all 3 `ISettings` extended with `IEmailSettings`, DI registration across Api/Web/Functions, AppHost wiring.

**Findings:**
- ✅ All core patterns correct: Base64 via `AddMessageWithBase64EncodingAsync`, queue constant used, `[LoggerMessage]` pattern, DI scoping, AppHost `WithReference`
- ⚠️ `EmailTemplateManager._logger` injected but never used → CS0414 warning. Recommend removing the logger or adding logging calls.
- ⚠️ `EmailTemplateManager` uses old-style constructor (not primary), inconsistent with `EmailSender`. Not a blocker.
- ⚠️ `AzureCommunicationsConnectionString` in settings unused by this PR — forward-looking for #618.

**Inbox:** `.squad/decisions/inbox/neo-617-review.md`

---

### 2026-04-02: RBAC Phase 2 — Pre-PR Code Review (Issue #607)

**Branch:** `squad/rbac-phase2`
**Scope:** Role management UI, ownership-based delete, CreatedByEntraOid flow end-to-end
**Test count:** 96 — all passing

**Verdict: REQUEST CHANGES — 2 critical issues must be fixed before merge.**

---

#### 🔴 Critical Issues (Block Merge)

**1. OID claim type inconsistency — security bypass on pre-Phase-2 records**

All three content controllers (`EngagementsController`, `SchedulesController`, `TalksController`) use `User.FindFirstValue("oid")` (short JWT claim name) for the ownership check. The project's own constant `ApplicationClaimTypes.EntraObjectId` is `"http://schemas.microsoft.com/identity/claims/objectidentifier"` (URI form). The `AdminController` uses this constant correctly.

- If the JWT middleware maps `oid` → URI form (standard .NET behavior), `currentUserOid` is `null` at runtime.
- When `currentUserOid == null` AND `record.CreatedByEntraOid == null` (all pre-Phase-2 records): `null != null` evaluates to `false` → `Forbid()` is **never called** → any Contributor can delete **all legacy records**.
- When `currentUserOid == null` AND record has a real OID: `null != "some-oid"` is `true` → Forbid() always fires → Contributor can **never** delete their own new records.

Fix required: Replace `User.FindFirstValue("oid")` with `User.FindFirstValue(ApplicationClaimTypes.EntraObjectId)` in all three controllers. Also add an explicit null guard: if `currentUserOid` is null or `record.CreatedByEntraOid` is null, return `Forbid()`.

Files: `EngagementsController.cs:139,200`, `SchedulesController.cs:161,196`, `TalksController.cs:109,151`

**2. TalksController.Delete performs deletion on HTTP GET — no CSRF protection, no confirmation**

`TalksController.Delete` is decorated `[HttpGet]` and immediately executes `DeleteEngagementTalkAsync`. Both `EngagementsController` and `SchedulesController` correctly use `[HttpGet]` for the confirmation view and `[HttpPost][ValidateAntiForgeryToken][ActionName("Delete")]` for `DeleteConfirmed`. `TalksController` skips this entirely — a crafted GET link on any page can silently delete a talk without user confirmation or CSRF token.

Fix required: Separate into GET (confirmation view) + POST `DeleteConfirmed` with `[ValidateAntiForgeryToken]`, matching the pattern of the other controllers.

File: `TalksController.cs:96–125`

---

#### 🟡 Minor Issues (Non-blocking)

**3. Tests use short `"oid"` claim form; AdminControllerTests uses URI form** — inconsistency means ownership tests pass in a way that may not reflect production behavior. After the critical fix above, tests should also be updated to use `ApplicationClaimTypes.EntraObjectId` for the `"oid"` claims.

**4. EF entity vs Domain nullable inconsistency (pre-flagged)** — `Data.Sql/Models/*.cs` use `string` (non-nullable, `#nullable disable`) while Domain models use `string?`. The DB column IS nullable. EF Core honors `#nullable disable` so this works at runtime, but creates misleading type signals for developers.

**5. ManageRolesViewModel leaks Domain model** — `CurrentRoles` and `AvailableRoles` are `IList<Role>` where `Role` is from `Broadcasting.Domain.Models`. Web layer convention is to use ViewModels throughout; a `RoleViewModel` should be introduced.

**6. No self-demotion guard in RemoveRole** — an Administrator can inadvertently remove their own Administrator role with no confirmation or prevention. CSRF is protected by `[ValidateAntiForgeryToken]`, but no business-rule guard exists.

**7. GetCalendarEvents() now requires auth (pre-flagged)** — class-level `[Authorize(RequireContributor)]` on `EngagementsController` gates this endpoint. If any public calendar widget or unauthenticated consumer called this endpoint pre-Phase-2, it will silently break.

**8. Add endpoints don't null-guard OID** — If `FindFirstValue(...)` returns null, `CreatedByEntraOid` is silently stored as null, making the record immediately un-deletable by Contributors. Should log a warning or fail fast.

---

#### 💡 Suggestions for Future Work

- **Centralize ownership checks** into a resource-based `IAuthorizationHandler<ContentOwnershipRequirement>` rather than repeating the pattern in each controller.
- **Introduce `RoleViewModel`** to keep the Web project's dependency graph clean.
- **Add migration idempotency test** — wire the SQL migration file into the CI pipeline's integration test run to catch regressions.
- **Phase 2.5 backfill** (already noted in migration SQL) — once historical data is available, backfill `CreatedByEntraOid` on existing records so Contributors can manage their pre-Phase-2 content.

---

**Files reviewed:** migration SQL, `BroadcastingContext.cs`, all 4 domain + EF entity models, `IUserApprovalManager`, `UserApprovalManager`, 6 controllers, `WebMappingProfile.cs`, `ManageRolesViewModel.cs`, `TalkViewModel.cs`, both Admin views, all 6 controller test files.

---

### 2026-04-02: RBAC Phase 1 — PR #610 Created and Reviewed

**PR:** [#610](https://github.com/jguadagno/jjgnet-broadcast/pull/610) — `feat: RBAC Phase 1 - User Approval & Role-Based Access Control`
**Branch:** `squad/rbac-phase1` → `main`
**Closes:** #602, #603, #604, #605, #606

**What was delivered (46 files, 3,646 insertions):**
- DB migration: `ApplicationUsers`, `Roles`, `UserRoles`, `UserApprovalLog` tables + 3 role seeds
- Domain: models, enums, constants, interfaces for the full approval workflow
- Data.Sql: EF Core repositories + `RbacProfile` AutoMapper mappings
- Managers: `UserApprovalManager` with approve/reject/role-assign audit trail
- Web Auth Pipeline: `EntraClaimsTransformation` (IClaimsTransformation) + `UserApprovalMiddleware`
- Web UI: `AccountController`, `AdminController`, 3 views, 3 ViewModels
- Tests: 37 new tests (5 classes); 631 total passing, 0 failing

**Round 1 Review Verdict: ⚠️ CHANGES REQUESTED**

Review posted as comment (GitHub blocks self-review): https://github.com/jguadagno/jjgnet-broadcast/pull/610#issuecomment-4174117340

**Blocking findings:**

| # | Severity | File | Issue |
|---|----------|------|-------|
| 1 | 🔴 HIGH | `Program.cs` | `UseUserApprovalGate()` placed AFTER `UseAuthorization` — pending users hit 403 before approval gate fires. Fix: move gate before `UseAuthorization`. |
| 2 | 🟠 MEDIUM | `AdminController.cs` | `GetAllUsersAsync()` loads all users into memory, then filters in C# — violates DB-layer filtering convention. Fix: add `GetUsersByStatusAsync` to manager/data store. |
| 3 | 🟠 MEDIUM | `EntraClaimsTransformation.cs` | Takes `IRoleDataStore` directly — Web layer calling Data layer, bypassing Managers. Fix: expose `GetRolesForUserAsync` on `IUserApprovalManager`. |

**Non-blocking findings:**
- Dead code: `approval_notes` claim read in `AccountController.Rejected()` but never populated by `EntraClaimsTransformation`
- `EntraObjectIdClaimType` constant duplicated in 2 files — should be in `Domain/Constants/`

**Scribe tasks completed:** `.squad/decisions/inbox/` (8 files) merged into `decisions.md`, committed.

---

### 2026-04-02: RBAC Phase 1 — PR #610 Round 2 Re-Review

**Commits reviewed (in order):**
- `22ad9a7` — Trinity: all 5 Round 1 findings fixed
- `06fbb77` — Tank: updated RBAC tests (GetUserRolesAsync, approval_notes claim, DB-level filtering mock)
- `c77d9d3` — Morpheus: base schema scripts updated (table-create.sql, data-create.sql)
- `56ab6be` — Tank: history update
- `5f3eeb3` — Trinity: BroadcastingContext DI fix in Web Program.cs

**Test results:** 84/84 Web tests pass, 76/76 Managers tests pass (0 failures)

**All 5 Round 1 findings verified resolved:**

| # | Finding | Verified |
|---|---------|---------|
| 1 | `UseUserApprovalGate()` before `UseAuthorization()` | ✅ Program.cs lines 149–150 |
| 2 | `AdminController.Users()` uses `GetUsersByStatusAsync()` | ✅ 3 DB-level calls |
| 3 | `EntraClaimsTransformation` uses `IUserApprovalManager` only | ✅ `GetUserRolesAsync()` |
| 4 | `approval_notes` claim populated for rejected users | ✅ Lines 63–67 |
| 5 | `ApplicationClaimTypes` constants in Domain | ✅ Partial — middleware missed |

**New additions verified:**
- `table-create.sql` RBAC tables ✅
- `data-create.sql` 3 role seeds ✅
- `BroadcastingContext` DI in Web Program.cs line 61 ✅

**Round 2 Review Verdict: ⚠️ CHANGES REQUESTED**

Review posted: https://github.com/jguadagno/jjgnet-broadcast/pull/610#issuecomment-4174225355

| # | Severity | File | Issue |
|---|----------|------|-------|
| NEW 1 | 🟠 MEDIUM (BLOCKING) | `UserApprovalMiddleware.cs` line 11 | Local `"approval_status"` const — not updated when finding #5 was fixed. Latent gate-bypass bug if `ApplicationClaimTypes.ApprovalStatus` changes. Fix: use `ApplicationClaimTypes.ApprovalStatus`. |
| NEW 2 | 🟡 Low (non-blocking) | Test files (3) | Hardcoded claim strings instead of `ApplicationClaimTypes` constants |
| NEW 3 | 🟡 Low (non-blocking) | `table-create.sql` + migration | Missing SQL CHECK constraints on `ApprovalStatus` and `Action` columns |

**Approved once NEW #1 is fixed. Ready for @jguadagno review and merge.**

---

### 2026-04-02: RBAC Phase 1 — PR #610 Round 3 Final Sign-off

**Head commit reviewed:** `d0aa61a` (Trinity: all 3 Round 2 findings fixed)

**Round 2 findings — all verified resolved:**

| # | Finding | Verified |
|---|---------|---------|
| NEW 1 (BLOCKING) | `UserApprovalMiddleware.cs` — `ApplicationClaimTypes.ApprovalStatus` used (line 49), local const gone | ✅ |
| NEW 2 (non-blocking) | Test files — `ApplicationClaimTypes.*` constants throughout (0 hardcoded strings) | ✅ |
| NEW 3 (non-blocking) | `table-create.sql` lines 196, 235 + migration lines 94–113 — idempotent CHECK constraints | ✅ |

**Sanity pass — clean:**
- Middleware order: `UseAuthentication` → `UseUserApprovalGate` → `UseAuthorization` ✅
- `EntraClaimsTransformation`: IUserApprovalManager only, ApprovalNotes populated for rejected users ✅
- `UserApprovalManager`: all 8 ops, full arg validation, audit trail ✅
- `AdminController`: `[Authorize(Policy="RequireAdministrator")]`, `[ValidateAntiForgeryToken]`, DB-level filtering ✅
- `ApplicationClaimTypes.cs`: single source of truth ✅

**New non-blocking observation (Phase 2):**
- `RejectUserViewModel.cs` is dead code — `AdminController.RejectUser()` binds to plain parameters, not to the ViewModel. Validation still correct via server-side null guard + HTML `required` attr. No security impact.

**Round 3 Verdict: ✅ APPROVED**

Review posted: https://github.com/jguadagno/jjgnet-broadcast/pull/610#issuecomment-4174260374

---

### Prior Work Archive (Sprint 11 + Early Reviews)

- **Sprint 11 closeout (PRs #551–#555):** 5 PRs merged, later **reverted** (PR #572, MSAL auth broken). Issue #85 open.
- **PR #557 ✅ APPROVED:** CI deployment approval gate + staging slot stop. Non-blocking: step ordering in API/Web (stop should precede get-URL step). Functions workflow had correct order.
- **PR #559 ✅ APPROVED:** Twitter integration tests — all 11 scope items verified. Joseph merged, issue #558 closed.

## Learnings

### 2026-04-05: Issue #637 — Bicep IaC Azure Access Assessment

- **Key finding:** ~40% of Azure resource names are documented (App Service names, Function app name, region, SKU, queue names, topic names, SQL DB name). ~60% are NOT (resource group name, SQL Server FQDN, Storage Account name, Key Vault name, App Insights workspace, Managed Identity names).
- **Pattern:** `infrastructure.md` + `.github/workflows/*.yml` give you logical resource names but NOT the physical Azure identifiers needed for Bicep parameterization.
- **Recommendation:** Azure **Reader** role on the production resource group is the minimum needed to avoid drift. `az group export` + Bicep decompiler is the fastest path to accurate templates.
- **Safe access level:** Reader cannot read Key Vault secret values or storage keys — safe to grant without exposing secrets.

### 2026-04-05: Issue #639 — EF Core bool/HasDefaultValueSql Warning

- **Key finding:** `BroadcastingContext.cs` configures `ScheduledItem.MessageSent` with `.HasDefaultValueSql("0")`. EF Core 8+ warns on non-nullable `bool` + DB default because it cannot distinguish explicit `false` from CLR default `false`.
- **Fix:** Remove `.HasDefaultValueSql("0")` from the `MessageSent` property configuration — it is redundant since EF Core always inserts the explicit C# value for all mapped properties. No behavioural regression.
- **Pattern:** When a `bool` property has a DB default of `0`/`false`, the `.HasDefaultValueSql()` call is redundant and should be omitted to silence EF Core sentinel warnings.
- **Assigned:** Trinity (EF Core data layer). XS effort.
- **Key files:** `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs`, `src/JosephGuadagno.Broadcasting.Data.Sql/Models/ScheduledItem.cs`

---

## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only

---

## Learnings — Bicep IaC (from PR #645 review, 2026-04-05)

- **Circular dependency pattern to avoid:** Never wire Module A → Module B where Module B also → Module A. The most common trap: App Services needing Key Vault URI *and* Key Vault needing App Service principal IDs. Solve by splitting into two passes: deploy compute first (without Key Vault URI in settings), then grant RBAC, then update settings in a second deployment.
- **Bicep module outputs cannot be `@secure()`**: Any `listKeys()` / connection string built in a module output is exposed in ARM deployment history. Keep secret assembly inside the module or push to Key Vault immediately. Only pass Key Vault secret references (`@Microsoft.KeyVault(...)`) through app settings.
- **Detect dead parameters before review:** If a module declares a parameter but never references it in any resource property, it is dead code AND potentially the root of false dependencies (e.g. `keyVaultUri` in app-service.bicep).
- **API version hygiene:** Always prefer GA over `-preview` for production Bicep templates. Preview APIs can have breaking changes without notice.
- **`allowBlobPublicAccess`**: Default should be `false` for all storage accounts unless there is an explicit public content requirement (e.g., CDN-served static files). Queue/table-only workloads should always be `false`.
- **`instrumentationKey` is deprecated:** App Insights connection string is the correct output; iKey should not be propagated or stored.
- **`kind: 'Storage'` for functions runtime storage is legacy:** Use `StorageV2` unless you have a specific reason for v1 blob-only storage.
- **Hardcoded email in IaC:** Alert notification addresses belong in parameters (per-environment), not hardcoded in `main.bicep`.


### 2026-04-06: .NET Technical Debt Sprint — #309, #311, #312

**Issues completed:**
- #309: IOptions refactor - Replaced manual Bind()+Singleton pattern with Configure<T>() + ValidateOnStart() across Api, Functions, Web
- #311: CancellationToken propagation - Added ct = default to all async manager/datastore methods, propagated to EF Core
- #312: OperationResult pattern - Introduced OperationResult<T> to replace throwing ApplicationException in managers

**PR:** #649 (squad/309-311-312-net-technical-debt → main)

**Key architectural changes:**
- IOptions pattern enforces early configuration validation via ValidateOnStart()
- CancellationToken support enables graceful shutdown in Azure Functions timer triggers
- OperationResult pattern makes success/failure contracts explicit, improves testability

**Testing:** All tests passing (0 failures). SyndicationFeedReader network test failures EXPECTED (external dependency).

**Next:** Sparks can start on #45, #46, #94, #102 (depend on stable manager interfaces).

## Learnings
### 2026-04-06: Functions DI Startup Failure (PR #649 Regression)
- **Root cause:** .ValidateOnStart() on AddOptions<T>() causes eager resolution of IOptions<T> during application startup
- **Symptom:** System.InvalidOperationException: A suitable constructor for type '_functionActivator' could not be located in Azure Functions isolated worker process
- **Why it breaks:** Azure Functions DI container setup happens in phases. ValidateOnStart() forces immediate service resolution before all dependencies are fully wired, causing activation failures
- **When it's safe:** ValidateOnStart() works when:
  1. The settings class has DataAnnotation attributes ([Required], [StringLength], etc.) that need validation
  2. You're NOT in an Azure Functions isolated worker (Api/Web projects can use it safely)
  3. The settings have no external dependencies (e.g., not using IConfiguration lookups that might not be ready)
- **Fix:** Remove .ValidateOnStart() from Functions project. Keep ValidateDataAnnotations() for runtime validation on first access
- **Prevention:** When refactoring DI registrations, test in the actual deployment target (Azure Functions runtime), not just local builds. Eager validation (ValidateOnStart) should only be used when you have actual DataAnnotations AND can guarantee the DI container is fully initialized

