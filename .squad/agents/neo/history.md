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

**Current focus:** API and Web health check implementation complete. PRs #640 (EF Core fix) and #641 (health checks) both approved.

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

### 2026-04-02: RBAC Phase 2 — Pre-PR Code Review (Summary)

**Branch:** squad/rbac-phase2 — 96 tests passing  
**Verdict:** REQUEST CHANGES — 2 critical security issues:
1. OID claim type inconsistency (`"oid"` vs URI form) — ownership check bypass on legacy records
2. TalksController.Delete uses GET without CSRF protection

7 additional minor issues flagged (entity/Domain nullable mismatch, ViewModel leak, self-demotion guard, etc.). Requires fixes before merge.

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

### 2026-04-02: RBAC Phase 1 — PR #610 Complete (Summary)

**PR:** #610 — User Approval & Role-Based Access Control (RBAC Phase 1)  
**Branch:** squad/rbac-phase1 (merged to main)  
**Test count:** 128 total, all passing after fixes

**Review summary (3 rounds):**
- **Round 1:** 5 findings (middleware order, manager methods, claim population) — all fixed
- **Round 2:** 3 new findings (1 BLOCKING: middleware const, 2 non-blocking: tests, CHECK constraints) — all fixed
- **Round 3:** Final sign-off confirmed. Dead code identified (`RejectUserViewModel`) deferred to Phase 2.

**Key learnings:** Middleware ordering (`Auth` → `UserApprovalGate` → `Authorization`), claim constant naming, idempotent SQL constraints, test consistency.

**Final verdict:** ✅ APPROVED — PR #610 merged.

---

### Prior Work Archive (Sprint 11 + Early Reviews)

- **Sprint 11 closeout (PRs #551–#555):** 5 PRs merged, later **reverted** (PR #572, MSAL auth broken). Issue #85 open.
- **PR #557 ✅ APPROVED:** CI deployment approval gate + staging slot stop. Non-blocking: step ordering in API/Web (stop should precede get-URL step). Functions workflow had correct order.
- **PR #559 ✅ APPROVED:** Twitter integration tests — all 11 scope items verified. Joseph merged, issue #558 closed.

## Learnings

## Recent Learnings

**PR #640 (2026-04-06) — EF Core value type defaults:** Never use `.HasDefaultValueSql()` on non-nullable value types — redundant and triggers EF Core 8+ warnings. EF Core always inserts the C# value.

**PR #641 (2026-04-06) — ServiceDefaults health checks:** Use conditional registration based on connection string presence. Allows safe sharing across Api, Web, Functions. Endpoint semantics: `/health` = readiness (all deps), `/alive` = liveness (self only).

**Issue #642 (2026-04-06) — Health check scope rules:**
- Table Storage and ACS go in ServiceDefaults (both Api and Web reference them).
- Key Vault goes in Web/Program.cs only (Api has no Key Vault SDK or config section).
- No official NuGet package exists for ACS health checks — always write a custom `IHealthCheck`.
- ACS health check must return `Degraded` (not `Unhealthy`) — email is non-critical for readiness.
- Key Vault health check should include `timeout: TimeSpan.FromSeconds(5)` — Key Vault calls average ~200ms.
- Config key for Table Storage logging: `Settings:LoggingStorageAccount`. Config key for ACS: `Email:AzureCommunicationsConnectionString`. Key Vault section: `KeyVault` (key `KeyVault:vaultUri`).
- `AspNetCore.HealthChecks.AzureStorage` 7.0.0 already installed in ServiceDefaults from PR #641 — no new package needed for Table Storage check.


