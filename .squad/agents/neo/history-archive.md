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

## Learnings

### Architecture & DateTime Conventions (2026-01-09)
- **Web → Data.Sql reference is a violation:** Web projects must never reference Data.Sql directly, only Managers. API projects are allowed to reference Data.Sql for DI setup.
- **DateTimeOffset is the standard:** All datetime handling should use `DateTimeOffset`, not `DateTime`. This applies to domain models (✅ compliant), ViewModels (✅ compliant), but Azure Functions have 30+ violations using `DateTime.UtcNow` instead of `DateTimeOffset.UtcNow`.
- **Manual LINQ .Select() is acceptable for anonymous types:** When creating anonymous objects for JSON serialization (e.g., FullCalendar.js), manual `.Select(x => new { ... })` is allowed since AutoMapper doesn't support anonymous types.
- **CancellationToken optional in Controllers:** While Manager classes correctly implement `CancellationToken cancellationToken = default`, Web Controllers currently don't accept CancellationTokens. This is acceptable but could be improved for better request cancellation handling.

## Recent Work

### 2026-01-09: Architecture & Conventions Audit

**Audit completed:** Pre-feature codebase health check across 6 dimensions  
**Findings:** 2 critical violations, 3 high-priority issues, 1 medium-priority improvement

**Critical violations:**
1. Web project has direct ProjectReference to Data.Sql (line 70 of Web.csproj) — bypasses Manager layer
2. Functions use `DateTime.UtcNow` instead of `DateTimeOffset.UtcNow` (30+ occurrences)

**High priority:**
- Web Controllers use `DateTime.UtcNow` for ViewModel initialization (2 occurrences)
- Should be `DateTimeOffset.UtcNow` to match property types

**Positive findings:**
- ✅ Domain models: All use DateTimeOffset consistently
- ✅ AutoMapper: Properly configured, consistent usage
- ✅ Async naming: All methods have `Async` suffix
- ✅ Error handling: Functions follow EventPublishException pattern with structured logging
- ✅ Recent changes (Social Media Platforms feature): Clean, no violations introduced

**Audit rating:** B+ (Good with Minor Issues)

**Decision inbox updated:** `neo-arch-audit-findings.md` created with full report and recommendations

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

### 2026-04-08: Epic #667 — Architecture Decisions Resolved

**Issue:** #667 — Move social links for engagements into its own table

Joseph answered all 6 open architecture questions. Updated GitHub issue and recorded decisions.

**Resolved decisions:**

1. **Talks**: Inherit social media from parent Engagement — no separate junction table
2. **EngagementSocialMediaPlatforms**: EngagementId FK + SocialMediaPlatformId FK + Handle (conference's @handle). SocialMediaPlatforms.Url = canonical platform URL
3. **ScheduledItems.Platform**: DROP column, ADD SocialMediaPlatformId int FK — intentional breaking change, migration script required
4. **MessageTemplates.Platform**: Migrate to SocialMediaPlatformId FK — currently in composite PK, requires careful migration planning
5. **Soft delete**: IsActive (bool). UI: ✗ icon for inactive. List page: single toggle button
6. **Seed data**: Twitter/X, BlueSky, LinkedIn, Facebook, Mastodon (even without publisher)

**Actions taken:**
- Updated GitHub issue #667: replaced Open Questions with Architecture Decisions table, added Implementation Order section, updated Work Breakdown (Mastodon added to seed data)
- Wrote decisions to `.squad/decisions/inbox/neo-667-architecture-decisions.md`

**Status:** ✅ All decisions recorded. Morpheus can begin DB work.

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

### 2026-04-09: PR #662 Re-Review — Junction Table Normalization (#323)

**Context:** Reviewed PR #662 (feat(data): normalize Tags column to junction table) after Morpheus + Trinity addressed all 6 items from original review (3 critical, 3 suggestions).

**Critical issues verified resolved:**
1. **EF SourceType bleed**: Originally EF navigation properties didn't filter by SourceType — reads would return mixed SyndicationFeed + YouTube tags for same ID. **Fix:** All reads now use direct discriminated queries (`broadcastingContext.SourceTags.Where(st => st.SourceId == id && st.SourceType == type)`). Zero Include(s => s.SourceTags) usage remains. Navigation properties retained for write-only operations (SyncSourceTagsAsync). BroadcastingContext includes WARNING comments on nav property configs.

2. **Transaction safety**: Originally SaveAsync called SaveChangesAsync twice (entity save + SyncSourceTagsAsync) without transaction wrapper — partial failure risk. **Fix:** Both data stores now wrap in BeginTransactionAsync/CommitAsync. No partial-failure window remains.

3. **EF model ambiguity**: Dual .WithOne() on same FK column risk — resolved via discriminated direct queries. Nav properties no longer used for reads so data integrity preserved.

**Suggestions verified implemented:**
- S1: Unique index UX_SourceTags_SourceId_SourceType_Tag added to migration + EF config
- S2: STRING_SPLIT SQL Server 2016+ compatibility documented
- S3: Trinity verified all 15 BuildHashTagList callers correct (compiler-enforced)

**Verdict:** APPROVED. Posted approval comment to PR #662, marked as ready for review. Ready for Joseph's merge decision.

**Pattern learned:** When using discriminated shared junction tables (SourceType column), prefer direct DbSet<JunctionEntity>.Where(filter) queries over navigation properties to enforce discriminator at query time. Navigation properties remain useful for writes (AddRange/RemoveRange operations).

**Files reviewed:**
- BroadcastingContext.cs (lines 241-248, 286-293, 296-316)
- SyndicationFeedSourceDataStore.cs (all read methods + SaveAsync)
- YouTubeSourceDataStore.cs (all read methods + SaveAsync)
- 2026-04-09-sourcetags-junction.sql (migration script)

**Decision document:** `.squad/decisions/inbox/neo-pr662-approved.md`

---

## Learnings — Epic #667 Triage: Social Media Platforms (2025-07-17)

**Context:** Triaged epic #667 — "Move social links for engagements into its own table".

**Codebase facts established:**
- `dbo.Engagements` has 3 social columns to remove: `BlueSkyHandle`, `ConferenceHashtag`, `ConferenceTwitterHandle`
- `dbo.Talks` has its own `BlueSkyHandle` — relationship to this epic needs clarification
- `dbo.ScheduledItems.Platform` is nvarchar(50) free-text — possible FK migration candidate
- `dbo.MessageTemplates.Platform` is part of the composite PK — high-impact if migrated
- Migration naming convention confirmed: `YYYY-MM-DD-description.sql`
- Sub-issues #537, #536, #54, #53 are all superseded by this epic (commented on all four)

**Squad assignments made on #667:**
- Morpheus: DB (tables, migrations, seed, EF Core)
- Trinity: API (CRUD endpoints, DTO updates)
- Switch: Web/Controllers (admin UI, engagement controller)
- Sparks: Views (Razor views, Bootstrap, JS)
- Tank: Tests

**Open questions documented** in `.squad/decisions/inbox/neo-social-media-platforms-epic.md` — must be resolved before DB work starts (especially junction table shape and ScheduledItems/MessageTemplates Platform migration strategy).


=======
## Learnings — Epic #667 Sprint Breakdown (2025-01-23)

**Task:** Break epic #667 into 15 child issues and organize into 3 sprints.

**Execution:**
- Created issues #668–#682 using `gh issue create` with full acceptance criteria, dependencies, and squad assignments
- Sprint 1 (6 issues): Database foundation + EF Core layer — all Morpheus
- Sprint 2 (4 issues): API endpoints + Manager layer — all Trinity
- Sprint 3 (5 issues): Web UI (Switch+Sparks), Tests (Tank), Cleanup (Neo)
- Added comment to #667 with full task list
- Decision doc written to `.squad/decisions/inbox/neo-667-sprint-breakdown.md`

**Key principles applied:**
- Sequential sprints with hard dependencies: Sprint 2 requires Sprint 1 complete, Sprint 3 requires Sprint 2 complete
- Database/domain first, API second, UI third — standard vertical slice order
- Tests and cleanup in final sprint to validate complete implementation
- Clear squad boundaries: Morpheus owns all data layer, Trinity owns all API/manager, Switch+Sparks own all Web UI

**Issue creation pattern:**
- Title format: `{Layer} — {Description}` (e.g., "Database — Create SocialMediaPlatforms tables")
- Body sections: Sprint, Description, Acceptance Criteria (checklist), Dependencies, Related, Assigned To
- Labels: `enhancement`, `.NET`, and appropriate `squad:{name}` labels
- Dependencies explicitly called out using issue numbers

**Validation:**
- All 15 issues created successfully
- Comment added to #667 with full breakdown
- Decision doc created
- History updated (this entry)



## Learnings — Epic #667 PR Review and Deployment Runbook (2026-04-08)

**Task:** Review Morpheus's PR on branch `issue-667-social-media-platforms` and write production deployment runbook.

**Context:**  
- Morpheus completed database layer work for Epic #667 (Social Media Platforms)  
- Branch exists locally (commit 3fc341e) but PR not yet created  
- Work introduces breaking changes to `MessageTemplate` interface affecting Api, Web, Functions  

**Review Findings:**

**✅ PASSES:**
1. **Database schema** — All tables match architecture decisions exactly (SocialMediaPlatforms, EngagementSocialMediaPlatforms, ScheduledItems/MessageTemplates FKs)
2. **SQL migration script** — Excellent quality, proper 7-part structure, correct PK rebuild sequence for MessageTemplates
3. **EF Core entities** — Match SQL schema perfectly, proper nullable annotations
4. **Domain models** — Correct nullability, Required attributes, proper navigation properties
5. **Repository pattern** — ISocialMediaPlatformDataStore interface complete, soft delete implemented correctly
6. **AutoMapper profiles** — Bidirectional mappings for both new entities
7. **DI registration** — Registered in Api Program.cs
8. **Base scripts updated** — table-create.sql and data-seed.sql reflect post-migration schema

**❌ BLOCKERS:**
1. **Build fails** — 14 compile errors across Data.Sql.Tests, Api, Web, Functions projects
2. **No PR exists** — Branch not pushed to GitHub
3. **Breaking change** — `IMessageTemplateDataStore.GetAsync` signature changed from `GetAsync(string platform, ...)` to `GetAsync(int socialMediaPlatformId, ...)`, breaking 4 Azure Functions

**Root cause of build errors:** Expected breaking change — downstream projects (Api, Web, Functions) still reference old `MessageTemplate.Platform` string field instead of new `SocialMediaPlatformId` int field. Requires Trinity and Cypher follow-up PRs.

**Recommendation:** CONDITIONAL APPROVAL pending:
1. Morpheus pushes branch and creates PR
2. Trinity updates Api layer (MessageTemplates endpoints, SocialMediaPlatforms CRUD)
3. Cypher updates all 4 Functions `ProcessScheduledItemFired` handlers
4. Switch updates Web layer (MessageTemplateService, Engagement controllers)
5. Build passes on main before DB migration runs

**Deployment Runbook:**  
Created comprehensive production deployment runbook posted to issue #667 ([comment link](https://github.com/jguadagno/jjgnet-broadcast/issues/667#issuecomment-4210318810)).

**Key runbook decisions:**
- **Downtime required:** 5-10 minute maintenance window during MessageTemplates PK rebuild (table lock)
- **Service stop requirement:** All services (Functions, Api, Web) must stop during PART 5 of migration
- **Deployment order:** Code MUST deploy first (all PRs merged), then DB migration during maintenance window
- **Safe vs. breaking:** Parts 1-3 (new tables + seed) are additive and safe; Parts 4-7 (column drops + PK rebuild) are breaking
- **Rollback plan:** Database restore from backup + redeploy previous code version
- **Risk mitigation:** Pre-flight checklist enforces "all code deployed first" rule

**Pattern established:**  
For breaking database migrations involving PK rebuilds or column drops:
1. **Code deploys first** — All layers (Data, Api, Web, Functions) must be updated and deployed
2. **Maintenance window required** — PK rebuild operations require brief downtime with services stopped
3. **Incremental migration option** — Additive changes (new tables, seed data) can run separately before code deployment
4. **Runbook mandatory** — Complex migrations require step-by-step runbook with rollback plan

**Files reviewed:**
- Migration script: `scripts/database/migrations/2026-04-08-social-media-platforms.sql` (279 lines)
- 24 C# files (753 insertions, 61 deletions)
- Base scripts: table-create.sql, data-seed.sql

**Outcome:**  
- ✅ Deployment runbook posted to #667  
- ✅ Review findings documented (neo-review-667.md)  
- ⏳ Awaiting Morpheus to push branch and create PR  
- ⏳ Awaiting Trinity/Cypher/Switch follow-up PRs to fix build errors  

**Next steps:**
1. Morpheus creates PR
2. Trinity/Cypher/Switch create follow-up PRs
3. Neo reviews final PR when build passes
4. Joseph executes deployment runbook during maintenance window


## Learnings — PR #683 Code Review (2026-04-11)

**Context:** Formal code review of Epic #667 PR #683 (SocialMediaPlatforms table and database layer). Comprehensive multi-sprint PR spanning Morpheus (Sprint 1 DB), Trinity (Sprint 2 API/Managers), and Tank (test fixes).

**Review scope:** 57 files, +2921/-244 lines. Migration script, domain models, data stores, manager, API controller, DTOs, AutoMapper profiles, DI registrations, scopes, Functions updates, test fixes.

**Key findings:**
1. ✅ **Architecture patterns respected:** Manager layer used correctly (Web/Functions never call data stores directly), soft delete via IsActive, DateTimeOffset consistency, DI registrations complete
2. ✅ **Migration script safety:** Adds nullable columns → populates → makes NOT NULL. MessageTemplates composite PK change handled without data loss. Idempotent and safe.
3. ✅ **Breaking change handling:** IMessageTemplateDataStore.GetAsync signature change (string→int) fixed in ALL callers (4 Functions, API, Web service)
4. ⚠️ **Minor inefficiency:** SocialMediaPlatformManager.GetByNameAsync loads all platforms for in-memory filtering (acceptable for 5 platforms, but pattern doesn't scale)
5. ⚠️ **Exception swallowing:** Data stores catch and return null/false without logging (suggested ILogger injection for troubleshooting)

**Verdict:** ✅ APPROVED — No blockers, production-ready. Two suggestions for future optimization (non-blocking).

**Pattern reinforced:**
- Multi-agent PR reviews require checking ALL layer interactions: DB → Data.Sql → Domain → Managers → API/Functions/Web
- Migration scripts must be verified for: idempotency, data loss risk, FK dependency order, nullable-first strategy
- Breaking interface changes require grep-based verification of ALL callers across solution
- Test compile errors from domain model changes are EXPECTED and must be fixed before merge (Tank's role)

**Tools used:** gh pr diff, view, grep, git diff --stat. Full diff was 5147 lines; reviewed in sections (migration script, interfaces, implementations, controllers, tests).

**Recommendation posted:** GitHub comment #4210546660 (cannot approve own PRs, posted as comment instead).
### 2026-04-09: PR #683 Code Review Complete — Epic #667 Consolidation

**Status:** ✅ CONSOLIDATED | Session log: .squad/log/2026-04-09T00-43-53Z-codeql-fixes.md

**Work Summary:**
- PR #683 (feat(#667): Add SocialMediaPlatforms table and database layer) — **APPROVED for merge**
- Verified all architectural patterns, migration script safety, breaking change handling
- Trinity executed CodeQL security hardening + performance suggestions from this review:
  - Log sanitization (5 CodeQL alerts fixed)
  - CSRF handling (1 CodeQL alert fixed)
  - DB-level name lookup (performance)
  - Exception logging (visibility)
- 3 inbox decisions merged to decisions.md (consolidating all team work)
- Appended team updates to Trinity, Neo, Tank history.md files

**Review Verified:**
- ✅ Build: 0 errors, 322 pre-existing warnings (safe)
- ✅ Architecture: Manager pattern respected, soft delete via IsActive
- ✅ Migration: Nullable-first, composite PK handled correctly, idempotent
- ✅ Breaking changes: All callers updated (4 Functions, API, Web)
- ✅ Test coverage: Tank fixed 40 compile errors
- ✅ Code quality: XML docs, AutoMapper, scopes, EF Core config complete

**Key Decisions Documented:**
1. **Log Sanitization Pattern** — Sanitize all user input before logging (prevents injection)
2. **JWT CSRF Handling** — `[IgnoreAntiforgeryToken]` for Bearer APIs (false positive suppression)
3. **DB Filtering** — GetByNameAsync delegates to data layer (performance + scalability)
4. **Exception Logging** — All data stores inject ILogger and log before returning null

**Next:** Joseph merges PR #683. Epic #667 Sprints 3-6 unblocked for Switch/Sparks (API/Web UI integration). Tank: Unit tests for SocialMediaPlatforms layer.
## Core Context

**Role:** Lead Reviewer & Architect | Architecture, code reviews, issue triage, sprint planning, CI/CD

**Established patterns:**
- DTO/API: request DTOs exclude route params, return Task<ActionResult<T>>, null guard before ToResponse
- Pagination: guard divide-by-zero, clamp page>=1, pageSize 1-100; Testing: sealed types use typed null
- EF Core value type defaults: Never .HasDefaultValueSql() on non-nullable value types
- Log sanitization: Strip \r\n from user input before logging (CodeQL injection prevention)
- JWT Bearer CSRF: [IgnoreAntiforgeryToken] at class level (NOT for cookie auth controllers)
- DB filtering: All lookups via data store methods, never in-memory at manager layer
- Breaking DB migrations (PK rebuilds): Code deploys first -> maintenance window -> migration script
- Functions DI: Remove .ValidateOnStart() from Functions projects (causes startup activation failures)
- Email queue: AddMessageWithBase64EncodingAsync (Base64 required for Azure Functions queue triggers)
- Ownership checks (tests): Must include OID claim on ControllerContext AND matching CreatedByEntraOid on mock entities
- Moq CancellationToken: Use non-generic Returns(Delegate) form with explicit matchers, not Returns<T1, T2>(lambda)

**Epic #667 Architecture Decisions:**
- SocialMediaPlatforms: Id, Name, Url, Icon, IsActive (soft delete)
- EngagementSocialMediaPlatforms: EngagementId+SocialMediaPlatformId+Handle (composite PK)
- ScheduledItems.Platform -> SocialMediaPlatformId int FK; MessageTemplates.Platform -> SocialMediaPlatformId (was composite PK)
- Seed: Twitter(1), BlueSky(2), LinkedIn(3), Facebook(4), Mastodon(5); Talks inherit from parent Engagement
- Sprint 1 DB+EF (Morpheus issues #668-#673), Sprint 2 API+Manager (Trinity #674-#677), Sprint 3 Web+Tests (Switch/Sparks/Tank/Neo #678-#682)

**IaC (Bicep):** Circular dependency: never Module A->B where B->A; listKeys() exposes secrets (use managed identity); StorageV2; ConnectionString over InstrumentationKey; Pin all API versions to GA (no -preview); allowBlobPublicAccess:false; event-grid.bicep is in modules/data/ not monitoring/

**Completed:** RBAC Phase 1&2 (PR #610,#611), Email (#623), Bicep IaC (PR #645 - CHANGES REQUESTED), Technical Debt PR #649, Junction table PR #662, Epic #667 PR #683 APPROVED

**Team standing rules:** Only Joseph merges PRs; All mapping via AutoMapper; Paging at data layer only

## 2026-04-10: Issue #695 - Replace DateTime.UtcNow with DateTimeOffset.UtcNow in Web Controllers

**Challenge:** Four Web controllers used `DateTime.UtcNow` instead of the team-standard `DateTimeOffset.UtcNow`, violating the codebase convention that all datetime operations must use `DateTimeOffset` to avoid timezone ambiguity.

**Solution:**
1. Replaced all 8 occurrences of `DateTime.UtcNow` across 4 Web controllers:
   - EngagementsController.cs line 206: 2 occurrences (StartDateTime, EndDateTime initialization)
   - SchedulesController.cs line 188: 1 occurrence (SendOnDateTime initialization)
   - TalksController.cs line 172: 2 occurrences (StartDateTime, EndDateTime initialization)
   - LinkedInController.cs lines 146, 155, 156: 3 occurrences (token expiration calculations)
2. For LinkedInController, added `.DateTime` property access when passing to KeyVault API (which expects `DateTime` parameter)

**Pattern:** When interfacing with legacy APIs that require `DateTime`, use `DateTimeOffset` for all calculations and convert at the boundary via `.DateTime` property.

**Result:** All Web controllers now use `DateTimeOffset.UtcNow` consistently. Build succeeds with 0 errors. PR #702 created.
## Learnings — Epic #667 PR Review and Deployment Runbook (2026-04-08)

**Task:** Review Morpheus's PR on branch `issue-667-social-media-platforms` and write production deployment runbook.

**Context:**  
- Morpheus completed database layer work for Epic #667 (Social Media Platforms)  
- Branch exists locally (commit 3fc341e) but PR not yet created  
- Work introduces breaking changes to `MessageTemplate` interface affecting Api, Web, Functions  

**Review Findings:**

**✅ PASSES:**
1. **Database schema** — All tables match architecture decisions exactly (SocialMediaPlatforms, EngagementSocialMediaPlatforms, ScheduledItems/MessageTemplates FKs)
2. **SQL migration script** — Excellent quality, proper 7-part structure, correct PK rebuild sequence for MessageTemplates
3. **EF Core entities** — Match SQL schema perfectly, proper nullable annotations
4. **Domain models** — Correct nullability, Required attributes, proper navigation properties
5. **Repository pattern** — ISocialMediaPlatformDataStore interface complete, soft delete implemented correctly
6. **AutoMapper profiles** — Bidirectional mappings for both new entities
7. **DI registration** — Registered in Api Program.cs
8. **Base scripts updated** — table-create.sql and data-seed.sql reflect post-migration schema

**❌ BLOCKERS:**
1. **Build fails** — 14 compile errors across Data.Sql.Tests, Api, Web, Functions projects
2. **No PR exists** — Branch not pushed to GitHub
3. **Breaking change** — `IMessageTemplateDataStore.GetAsync` signature changed from `GetAsync(string platform, ...)` to `GetAsync(int socialMediaPlatformId, ...)`, breaking 4 Azure Functions

**Root cause of build errors:** Expected breaking change — downstream projects (Api, Web, Functions) still reference old `MessageTemplate.Platform` string field instead of new `SocialMediaPlatformId` int field. Requires Trinity and Cypher follow-up PRs.

**Recommendation:** CONDITIONAL APPROVAL pending:
1. Morpheus pushes branch and creates PR
2. Trinity updates Api layer (MessageTemplates endpoints, SocialMediaPlatforms CRUD)
3. Cypher updates all 4 Functions `ProcessScheduledItemFired` handlers
4. Switch updates Web layer (MessageTemplateService, Engagement controllers)
5. Build passes on main before DB migration runs

**Deployment Runbook:**  
Created comprehensive production deployment runbook posted to issue #667 ([comment link](https://github.com/jguadagno/jjgnet-broadcast/issues/667#issuecomment-4210318810)).

**Key runbook decisions:**
- **Downtime required:** 5-10 minute maintenance window during MessageTemplates PK rebuild (table lock)
- **Service stop requirement:** All services (Functions, Api, Web) must stop during PART 5 of migration
- **Deployment order:** Code MUST deploy first (all PRs merged), then DB migration during maintenance window
- **Safe vs. breaking:** Parts 1-3 (new tables + seed) are additive and safe; Parts 4-7 (column drops + PK rebuild) are breaking
- **Rollback plan:** Database restore from backup + redeploy previous code version
- **Risk mitigation:** Pre-flight checklist enforces "all code deployed first" rule

**Pattern established:**  
For breaking database migrations involving PK rebuilds or column drops:
1. **Code deploys first** — All layers (Data, Api, Web, Functions) must be updated and deployed
2. **Maintenance window required** — PK rebuild operations require brief downtime with services stopped
3. **Incremental migration option** — Additive changes (new tables, seed data) can run separately before code deployment
4. **Runbook mandatory** — Complex migrations require step-by-step runbook with rollback plan

**Files reviewed:**
- Migration script: `scripts/database/migrations/2026-04-08-social-media-platforms.sql` (279 lines)
- 24 C# files (753 insertions, 61 deletions)
- Base scripts: table-create.sql, data-seed.sql

**Outcome:**  
- ✅ Deployment runbook posted to #667  
- ✅ Review findings documented (neo-review-667.md)  
- ⏳ Awaiting Morpheus to push branch and create PR  
- ⏳ Awaiting Trinity/Cypher/Switch follow-up PRs to fix build errors  

**Next steps:**
1. Morpheus creates PR
2. Trinity/Cypher/Switch create follow-up PRs
3. Neo reviews final PR when build passes
4. Joseph executes deployment runbook during maintenance window


## Learnings — PR #683 Code Review (2026-04-11)

**Context:** Formal code review of Epic #667 PR #683 (SocialMediaPlatforms table and database layer). Comprehensive multi-sprint PR spanning Morpheus (Sprint 1 DB), Trinity (Sprint 2 API/Managers), and Tank (test fixes).

**Review scope:** 57 files, +2921/-244 lines. Migration script, domain models, data stores, manager, API controller, DTOs, AutoMapper profiles, DI registrations, scopes, Functions updates, test fixes.

**Key findings:**
1. ✅ **Architecture patterns respected:** Manager layer used correctly (Web/Functions never call data stores directly), soft delete via IsActive, DateTimeOffset consistency, DI registrations complete
2. ✅ **Migration script safety:** Adds nullable columns → populates → makes NOT NULL. MessageTemplates composite PK change handled without data loss. Idempotent and safe.
3. ✅ **Breaking change handling:** IMessageTemplateDataStore.GetAsync signature change (string→int) fixed in ALL callers (4 Functions, API, Web service)
4. ⚠️ **Minor inefficiency:** SocialMediaPlatformManager.GetByNameAsync loads all platforms for in-memory filtering (acceptable for 5 platforms, but pattern doesn't scale)
5. ⚠️ **Exception swallowing:** Data stores catch and return null/false without logging (suggested ILogger injection for troubleshooting)

**Verdict:** ✅ APPROVED — No blockers, production-ready. Two suggestions for future optimization (non-blocking).

**Pattern reinforced:**
- Multi-agent PR reviews require checking ALL layer interactions: DB → Data.Sql → Domain → Managers → API/Functions/Web
- Migration scripts must be verified for: idempotency, data loss risk, FK dependency order, nullable-first strategy
- Breaking interface changes require grep-based verification of ALL callers across solution
- Test compile errors from domain model changes are EXPECTED and must be fixed before merge (Tank's role)

**Tools used:** gh pr diff, view, grep, git diff --stat. Full diff was 5147 lines; reviewed in sections (migration script, interfaces, implementations, controllers, tests).

**Recommendation posted:** GitHub comment #4210546660 (cannot approve own PRs, posted as comment instead).

## Learnings — PR #723 Code Review (2026-04-17)

**Context:** Code review of PR #723 implementing issue #719 (role hierarchy restructure). Renames existing `Administrator` → `Site Administrator` (full app admin) and introduces new narrower `Administrator` role (personal content admin).

**Review scope:** 14 files, +123/-29 lines. Domain constants, Program.cs policies, 3 controllers, 1 view, DB seed + migration script, 3 test files, 3 agent history files.

**Initial review:** CHANGES REQUESTED — SocialMediaPlatforms/Index.cshtml had role checks not updated.

**Follow-up (commit 2a6a15e):** Trinity fixed the view. Re-verified all 4 IsInRole locations:
- Line 10: `Site Administrator || Administrator || Contributor` ✅
- Line 68: `Site Administrator || Administrator || Contributor` ✅
- Line 85: `Site Administrator` only (Delete button) ✅
- Line 104: `Site Administrator || Administrator || Contributor` ✅

**Final verification:**
1. All view role checks align with controller authorization policies
2. Add/Edit/ToggleActive = RequireContributor (Site Admin + Admin + Contributor) — views match
3. Delete = RequireSiteAdministrator (Site Admin only) — view matches
4. Other SocialMediaPlatforms views (Add.cshtml, Edit.cshtml, Delete.cshtml) have no inline role checks — correct because authorization is enforced at controller action level
5. No other Razor views in the solution have orphaned role checks that need updating

**Key findings (all verified):**
1. **Domain constants** — `RoleNames.SiteAdministrator` added correctly
2. **Authorization policies** — Cumulative chain correct
3. **Controllers** — SiteAdminController, LinkedInController, SocialMediaPlatformsController all correct
4. **Views** — _Layout.cshtml and SocialMediaPlatforms/Index.cshtml both correct
5. **DB scripts** — Idempotent rename + seed pattern correct
6. **Self-demotion guard** — Uses `RoleNames.SiteAdministrator`
7. **Tests** — All policy assertions and fixtures updated

**Final Verdict:** ✅ **APPROVED**

### 2026-04-09: PR #683 Code Review Complete — Epic #667 Consolidation

**Status:** ✅ CONSOLIDATED | Session log: .squad/log/2026-04-09T00-43-53Z-codeql-fixes.md

**Work Summary:**
- PR #683 (feat(#667): Add SocialMediaPlatforms table and database layer) — **APPROVED for merge**
- Verified all architectural patterns, migration script safety, breaking change handling
- Trinity executed CodeQL security hardening + performance suggestions from this review:
  - Log sanitization (5 CodeQL alerts fixed)
  - CSRF handling (1 CodeQL alert fixed)
  - DB-level name lookup (performance)
  - Exception logging (visibility)
- 3 inbox decisions merged to decisions.md (consolidating all team work)
- Appended team updates to Trinity, Neo, Tank history.md files

**Review Verified:**
- ✅ Build: 0 errors, 322 pre-existing warnings (safe)
- ✅ Architecture: Manager pattern respected, soft delete via IsActive
- ✅ Migration: Nullable-first, composite PK handled correctly, idempotent
- ✅ Breaking changes: All callers updated (4 Functions, API, Web)
- ✅ Test coverage: Tank fixed 40 compile errors
- ✅ Code quality: XML docs, AutoMapper, scopes, EF Core config complete

**Key Decisions Documented:**
1. **Log Sanitization Pattern** — Sanitize all user input before logging (prevents injection)
2. **JWT CSRF Handling** — `[IgnoreAntiforgeryToken]` for Bearer APIs (false positive suppression)
3. **DB Filtering** — GetByNameAsync delegates to data layer (performance + scalability)
4. **Exception Logging** — All data stores inject ILogger and log before returning null

**Next:** Joseph merges PR #683. Epic #667 Sprints 3-6 unblocked for Switch/Sparks (API/Web UI integration). Tank: Unit tests for SocialMediaPlatforms layer.

## 2026-04-10: Issue #690 - Remove Web → Data.Sql Direct Reference

**Challenge:** Web project had illegal direct `<ProjectReference>` to Data.Sql, violating architectural rule that Web must NEVER call data stores directly.

**Solution:**
1. Removed Data.Sql reference from Web.csproj
2. Added Data.Sql reference to Managers.csproj (so Web gets transitive access)
3. Created ServiceCollectionExtensions.cs in Data.Sql with DI extension methods in Microsoft.Extensions.DependencyInjection namespace:
   - AddSqlDataStores() - registers all data store implementations
   - AddDataSqlMappingProfiles() - adds AutoMapper profiles
4. Updated Web/Program.cs to use extension methods, no direct Data.Sql types
5. Used fully-qualified type name for BroadcastingContext to avoid using statement

**Architecture:** Web → Managers → Data.Sql (transitive dependency only)

**Result:** Web code never directly references Data.Sql types. Architectural boundary enforced. Build succeeds. PR #700 created.

**Learnings:**
- Extension method namespace matters! Placing in Microsoft.Extensions.DependencyInjection makes them discoverable without needing project using statement.
- Transitive dependencies allow compile-time access to types without direct ProjectReference.
- Architectural rules are about preventing coupling in application code, not startup/DI configuration.

## Learnings — Issue #713 Code Review (2026-04-16)

**Task:** Review Trinity's exception audit work on branch `issue-713-audit-exceptions`.

**Files correctly modified by Trinity (6):**
- EngagementSocialMediaPlatformDataStore.cs — fixed 1 catch block
- FeedCheckDataStore.cs — added ILogger + 2 LogError calls
- ScheduledItemDataStore.cs — added ILogger + 2 LogError calls
- SyndicationFeedSourceDataStore.cs — added ILogger + 2 LogError calls
- TokenRefreshDataStore.cs — added ILogger + 2 LogError calls
- YouTubeSourceDataStore.cs — added ILogger + 2 LogError calls

**Files MISSED by Trinity (2) — BLOCKING:**
- EngagementDataStore.cs — 5 catch blocks without logging, no ILogger
- EngagementManager.cs — 2 catch blocks without logging, no ILogger

**Files already correct (not Trinity's work):**
- SocialMediaPlatformDataStore.cs — already had ILogger + logging
- EmailSender.cs — uses source-generated logging
- UserApprovalManager.cs — already had ILogger with LogWarning
- BroadcastingContext.cs — catch rethrows, not swallowing

**Build status:** 25 errors — test files not updated to pass ILogger mocks.

**Review pattern established:**
- For exception audit: grep `catch\s*\(.*Exception` and verify EVERY catch has logging
- When adding ILogger to primary constructors, MUST update test instantiations
- Run `dotnet build` before marking any code work complete
- Cross-check claimed scope against actual diff

**Verdict:** REJECTED — Assigned to Morpheus to fix (Trinity lockout per rejection rules).

## Learnings — PR #736 Code Review (2026-04-17)

**Context:** Review of PR #736 (feat(#728): Thread owner OID through manager business logic) — Sprint 17 of Epic #609 per-user data isolation.

**Scope:** 25 files, +448/-66 lines. Manager interfaces + implementations, reader interfaces + implementations, Functions Settings, collector Functions, and tests.

**Key review points verified:**
1. **Reader overload pattern:** New `ownerOid` overloads call parameterless version, then apply `ApplyOwnerOid()` helper that sets `CreatedByEntraOid = ownerOid`
2. **Manager pass-through:** All manager `ownerEntraOid` overloads are single-line delegations to data stores — no OID resolution logic
3. **Functions Settings:** `ISettings.OwnerEntraOid` added as `required string` with XML docs — fails fast if config missing
4. **Collector updates:** All 4 collectors (LoadAllPosts, LoadNewPosts, LoadAllVideos, LoadNewVideos) pass `settingsOptions.Value.OwnerEntraOid`
5. **Backward compatibility:** Parameterless methods preserved with `string.Empty` for admin/background processing contexts
6. **Test updates:** All 4 collector test files updated mock setups to pass `OwnerEntraOid` constant

**Pattern confirmed:**
- For owner-aware overloads: call existing parameterless method, then post-process to apply ownership
- This preserves existing behavior while adding new capability
- `required string` on Settings properties catches missing config at startup, not runtime

**Verdict:** ✅ APPROVED — Clean implementation, all acceptance criteria met, no invariant violations.

## Learnings — PR #739 Follow-up Review (2026-04-18)

**Context:** Re-review of PR #739 (feat(#729): enforce owner isolation in API controllers) after Tank added 11 security tests across 3 test files on branch `issue-729`. Previous rejection was for zero 403/ForbidResult and SiteAdmin bypass tests.

**Test run:** 84/84 green. All new tests pass correctly.

**What Tank got right:**
- `SchedulesController`: All 4 guarded actions covered (GetScheduledItem, UpdateScheduledItem, DeleteScheduledItem non-owner + GetAll SiteAdmin) ✅
- `MessageTemplatesController`: All 3 guarded actions covered (Get, Update non-owner + GetAll SiteAdmin) ✅ (new test file)
- `EngagementsController` top-level CRUD: All 4 covered (GetEngagement, UpdateEngagement, DeleteEngagement non-owner + GetEngagements SiteAdmin) ✅
- Correct pattern: entity OID ≠ user OID → `ForbidResult`, side-effectful calls verified `Times.Never` ✅
- No magic strings — `Domain.Constants.ApplicationClaimTypes.EntraObjectId`, `RoleNames.SiteAdministrator`, `Domain.Scopes.*` constants used ✅
- Moq `It.IsAny<CancellationToken>()` pattern correct ✅

**What Tank missed — BLOCKERS:**
`EngagementsController` has 9 more ownership-guarded actions with zero non-owner 403 tests:
- **Talks sub-actions (5):** `GetTalksForEngagementAsync`, `CreateTalkAsync`, `UpdateTalkAsync`, `GetTalkAsync`, `DeleteTalkAsync`
- **Platforms sub-actions (4):** `GetPlatformsForEngagementAsync`, `GetPlatformForEngagementAsync`, `AddPlatformToEngagementAsync`, `RemovePlatformFromEngagementAsync`
All 9 have the identical `if (!IsSiteAdministrator() && engagement.CreatedByEntraOid != GetOwnerOid()) return Forbid();` pattern, but no non-owner test exercises it.

**Verdict:** ❌ REJECTED — Talks and Platforms sub-resource ownership paths uncovered. PR comment #739 posted with specific tests required.

**Pattern reinforced:** When reviewing ownership-guarded controllers, grep for ALL `Forbid()` call sites, not just the primary CRUD actions. Sub-resource actions on the same entity share the same ownership gate and need the same test coverage.

## 2026-04-18: Sprint 18 Retrospective Debrief (Completed)

**Status:** ✅ COMPLETE  
**Session:** Retro follow-up with Joseph Guadagno

### Work Summary

Conducted Sprint 18 retrospective debrief with Joseph. All 18 retro items reviewed and categorized:
- 12 failures traced to **inadequate pre-submission validation** (PRs #738, #739 required multiple review rounds)
- 5 directive violations — "run tests before committing" was written but had no enforcement mechanism
- 6 HIGH severity items all related to missing security test coverage on a security feature

### Action Items Filed (Sprint 19)

All 6 action items filed as issues #743–#748 with `sprint:19` label:

**P1 (Training/Enforcement):**
- **#743:** Security test checklist skill (Tank) — Establish mandatory Forbid() coverage pattern
- **#744:** Test-pass gate before push (Neo) — Decide: training vs. enforcement? Branch protection? PR template? Pre-push hook?
- **#747:** Tank history checklist update (Tank) — Document ownership test checklist ownership

**P2 (Enhancement):**
- **#745:** Non-owner context helper (Trinity) — Add helper to API test base classes
- **#746:** PR comment formatting (Neo) — Improve clarity of code review feedback
- **#748:** Mock overload docs (Tank) — Document `.Setup()` pattern for controllers with ownerOid param

### Open Question for Joseph

**Should the "run tests before committing" gap be addressed as:**
1. **Training** — better checklists/tools for Tank (prioritize #743, #747 first), or
2. **Enforcement** — CI gate that blocks PRs with failures (prioritize #744 first)?

Joseph's answer determines P1 priority order.

### Decisions Documented

Tank's security checklist skill (@.squad/skills/security-test-checklist/SKILL.md) now captures the ownership enforcement pattern:
- Grep Forbid() sites first
- Build coverage matrix
- Write one test per Forbid() path
- Include matrix in PR description
- Run `dotnet test` with 0 failures before PR creation

**Permanent team rules established:**
1. ALWAYS run `dotnet test` before committing
2. ZERO test failures before opening PR
3. For any security/ownership feature: grep `Forbid()` first, build matrix, write test per site
4. When controller signatures add `ownerOid` parameter: update mock `.Setup()` overload immediately

### Outcome

Sprint 18 fully retrospected. Sprint 19 ready to address identified gaps. Awaiting Joseph's decision on enforcement approach for #744 to finalize P1 prioritization.

## 2026-04-18: Issue #744 Advisor Role (Background)

**Status:** ✅ COMPLETE (Background Agent)  
**Role:** Architecture advisor

### Work Summary

Advised Joseph on three options for implementing test-pass gate on PR #744:

1. **Branch Protection Rule** (GitHub UI)
   - Prevents merge until PR checks pass (including tests)
   - Built-in, no code needed
   - Requires GitHub repository admin access
   - Applies to all PRs uniformly

2. **PR Template** (Markdown file)
   - Prompts submitter to confirm tests were run
   - Non-blocking (user can ignore prompt)
   - Good for training/awareness
   - Works with existing CI/CD

3. **Pre-push Hook** (Git local setup)
   - Developer machine runs tests before push
   - Fastest feedback (before network round-trip)
   - Requires developer buy-in and proper setup
   - Can be bypassed with `git push --no-verify`

### Background Findings

Neo researched each option:
- Branch Protection: GitHub-native, strongest enforcement, requires admin
- PR Template: Low friction, good for onboarding, reminder-based
- Pre-push Hook: Fastest local feedback, but human-dependent

Joseph to decide which option(s) align with team workflow.

### Decision Impact

The chosen enforcement mechanism will shape Sprint 19 priority order:
- **Training-first approach:** #743 (checklist skill), #747 (Tank history) first, then consider #744
- **Enforcement-first approach:** Implement #744 first, then training artifacts as reinforcement

---

## Learnings — PR #756 Recovery Push (2026-04-19)

- Recovered the issue #731 fixes onto `issue-731-user-publisher-settings` safely via a dedicated worktree rooted at `.worktrees\issue-731-recovery`, leaving the original dirty branch untouched.
- The decisive integration fixes live in:
  - `src\JosephGuadagno.Broadcasting.Web\Services\UserPublisherSettingService.cs` — align Web `IDownstreamApi` calls to `/UserPublisherSettings`, map typed request/response DTOs, preserve masked write-only secrets.
  - `src\JosephGuadagno.Broadcasting.Api\Controllers\UserPublisherSettingsController.cs` and `src\JosephGuadagno.Broadcasting.Data.Sql\UserPublisherSettingDataStore.cs` — sanitize owner OIDs before warning/error logs.
  - `src\JosephGuadagno.Broadcasting.Web.Tests\Services\UserPublisherSettingServiceTests.cs`, `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\UserPublisherSettingsControllerTests.cs`, and `src\JosephGuadagno.Broadcasting.Data.Sql.Tests\UserPublisherSettingDataStoreTests.cs` — lock the contract with route, payload-shape, write-only masking, and log-sanitization coverage.
- For a PR authored by the same GitHub user account, Neo should leave a regular PR comment summarizing readiness instead of attempting a formal approval review.

---

## 2026-04-19 — Retro: Directive Drift and Token Waste

**Status:** ✅ COMPLETE  
**Scope:** Three-sprint retrospective with focus on Sprint 20 PR recovery (#756/#757)

### What Went Wrong

- Team-critical directives were documented, but not enforced at spawn
  time or push time.
- Review-state language drifted between "approved", "review comment",
  and "regular PR comment", creating confusion about whether the result
  was local-only or actually visible on GitHub.
- Branch-of-record drifted during Sprint 20 recovery: handoff text
  referenced `feat/731-user-publisher-settings`, while the actual PR
  branch of record was `issue-731-user-publisher-settings`.
- Expensive work started before cheap validation, so the team paid for
  re-reviews, repeat reads, and recovery orchestration.

### Root Causes

1. **No execution-time gate on directives** — rules in `decisions.md`,
   skills, and history were treated as reference material instead of
   hard preflight checks.
2. **No single operational source of truth per PR** — branch, review
   artifact type, blocker state, and next owner were spread across
   logs, decisions, and agent memory.
3. **Coordinator routing lacked a preflight contract** — agents were
   spawned without first fixing ambiguity around branch,
   GitHub-visible output, and readiness conditions.
4. **Terminology was loose** — "review", "comment", "approval", and
   "ready to merge" were used interchangeably even when they implied
   different external effects.

### Recommended Changes

1. **Hard rule:** Coordinator must set a preflight contract before
   every agent turn: branch of record, desired GitHub artifact,
   expected validation, and handoff owner.
2. **Hard rule:** Convert process-critical directives into verified
   gates. If the gate cannot be proven, the work does not start.
3. **Hard rule:** Maintain one PR state record with branch, artifact
   mode (`review` vs `comment`), blocker status, and latest owner;
   update it on every handoff.
4. **Hard rule:** Run cheap checks before expensive actions: local
   branch/readiness checks first, GitHub/API reads second, agent spawns
   last.
5. **Soft habit:** End each orchestration turn with an explicit "state
   changed" note so the next turn does not infer status from narrative
   text.

### Coordinator Direction

- Stop assuming agents will reconcile conflicting instructions on their
  own.
- Reject ambiguous requests before spawn and restate them as an
  execution contract.
- Treat repeated directive violations as process failures, not agent
  personality problems.

## Learnings — Issue #609 First-Round Review (2026-04-19)

- Reviewed epic #609 against its decomposition comment (#725-#732), merged
  commits on `main`, current repository code, and the squad audit notes from
  Trinity and Tank.
- Confirmed the repo has shipped the schema, owner-filtered query paths,
  API/Web ownership checks, per-user publisher-settings CRUD, and broad
  automated coverage for the first-round multi-tenancy work.
- Identified a blocking mismatch between the intended collector ownership flow
  and the shipped implementation: Functions still pass a single
  `Settings.OwnerEntraOid` value into collectors, and that setting is marked as
  temporary scaffold rather than per-collector ownership.
- Identified a second blocking mismatch in the readers: non-owner overloads in
  `SyndicationFeedReader` and `YouTubeReader` still materialize records with
  `CreatedByEntraOid = string.Empty`, which conflicts with the explicit
  first-round directive that persisted ownership must never be empty/null.
- Conclusion for Neo review: long-term epic scope is still intentionally
  deferred in several areas, but even the narrowed first-round slice should be
  treated as **not complete** until collector ownership is sourced from the
  collector record and the remaining empty-owner scaffolding is removed.

## Learnings — Issue #609 Gap Issue Triage (2026-04-19)

- Converted the remaining first-round #609 review gaps into three concrete issue
  proposals: two implementation follow-ups and one regression-test follow-up.
- No open duplicate issue exists for the collector-owner threading gap or the
  empty-owner reader scaffolding gap; the closest related item is closed issue
  #728, whose acceptance criteria already described the intended end state.
- Routing decision for the follow-up work: implementation belongs with Trinity
  under the canonical `.squad/routing.md` rule for Azure Functions/business
  logic, while the regression coverage belongs with Tank.



## Sprint 20 Conclusion — Final Review & Merge (2026-04-19T15:40:15Z)

**Decision Sources:** Inbox files processed by Scribe  

**Context Closure:**
- ✅ PR #756 merged to main (recovered issue #731: per-user publisher settings)
- ✅ PR #757 merged to main (issue #732: owner isolation test coverage)
- ✅ Decision inbox entries: link-main-pr-merge, neo-609-first-round-review, neo-609-gap-issues, neo-retro-directives merged to decisions.md
- ✅ All Sprint 20 work captured in .squad/orchestration-log/ and .squad/log/

**Impact on Future Sessions:**
- Retro analysis in decisions.md (link-retro-guardrails) identifies 3 review cycles due to incomplete submissions — recommend pre-execution checklist gate for Tank in next sprint
- Epic #609 first-round audit complete; data-layer test coverage gaps identified (Trinity recommendation: add 3–4 test cases per data store)

## Learnings — Issue #609 GitHub Gap Issues Created (2026-04-19)

- Confirmed the Round 1 #609 follow-up gaps had not been created as real GitHub issues yet; only the triage decision existed in squad notes.
- Created the three narrow GitHub issues needed to track closeout work: #760 (collector owner OID sourced from collector records), #761 (remove empty-owner reader scaffolding), and #762 (regression coverage for owner threading and persisted owner OIDs).
- Applied squad ownership labels per the earlier routing decision: Trinity owns the two implementation issues and Tank owns the regression-test issue.
