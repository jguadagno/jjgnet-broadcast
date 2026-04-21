## Core Context

**Key established patterns:**
- DTO/API: request DTOs exclude route params, return Task<ActionResult<T>>, null guard before ToResponse
- Pagination: guard divide-by-zero, clamp page>=1, pageSize 1-100; Testing: sealed types use typed null
- EF Core value type defaults: Never .HasDefaultValueSql() on non-nullable value types
- Log sanitization: Strip \r\n from user input before logging (CodeQL injection prevention)
- JWT Bearer CSRF: [IgnoreAntiforgeryToken] at class level (NOT for cookie auth controllers)
- DB filtering: All lookups via data store methods, never in-memory at manager layer
- Breaking DB migrations (PK rebuilds): Code deploys first → maintenance window → migration script
- Functions DI: Remove .ValidateOnStart() from Functions projects (causes startup activation failures)
- Email queue: AddMessageWithBase64EncodingAsync (Base64 required for Azure Functions queue triggers)
- Ownership checks (tests): Must include OID claim on ControllerContext AND matching CreatedByEntraOid on mock entities
- Moq CancellationToken: Use non-generic Returns(Delegate) form with explicit matchers, not Returns<T1, T2>(lambda)

**Epic #667 Architecture:**
- SocialMediaPlatforms: Id, Name, Url, Icon, IsActive (soft delete)
- EngagementSocialMediaPlatforms: EngagementId+SocialMediaPlatformId+Handle (composite PK)
- ScheduledItems.Platform → SocialMediaPlatformId int FK; MessageTemplates.Platform → SocialMediaPlatformId
- Seed: Twitter(1), BlueSky(2), LinkedIn(3), Facebook(4), Mastodon(5)

**Completed initiatives:**
- Sprint 8: DTO merge PR #512, pagination guards PR #514
- Sprint 9 PR #517: SQL Server 50MB cap removal, SaveChangesAsync error handling
- Sprint 10 PR #529: Social columns (BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle)
- RBAC #602: Roles, ApplicationUsers, UserRoles, UserApprovalLog; RBAC #607: CreatedByEntraOid columns
- PR #662 (#323): SourceTags junction discriminator + unique index UX_SourceTags_SourceId_SourceType_Tag
- PR #739 (2026-04-18): API ownership enforcement with 20 security test coverage
- PR #731/#732 (2026-04-19): Per-user publisher settings + owner isolation tests (Sprint 20 complete)

**Auth architecture:** Web layer uses battle-tested `EntraClaimsTransformation` (SQL-backed, app-managed roles); API will migrate from 42 custom Entra scopes to role-based policies matching Web pattern. Scope-to-role migration phases (#763–#769) scheduled across future sprints to prevent blocking.

**Sprint 21 scope:** Collector owner OID completeness (Trinity #760/#761, Tank regression #762). Blocker: `scripts\database\data-seed.sql` requires CreatedByEntraOid bootstrap values; PR #771 stacked on #770; PR #772 has unrelated Web config drift.

---

## 2026-04-20 — Branch/PR Policy Remediation

**Status:** ✅ COMPLETE (Remediation)

### Issue

Sprint 21 work was done directly on `main` instead of feature branches — third violation of `.squad/routing.md` directive.

### Actions

1. Stashed all uncommitted changes safely
2. Created stacked branches from origin/main:
   - `issue-761` → PR #770 (base: main)
   - `issue-760` → PR #771 (base: issue-761)
   - `issue-762` → PR #772 (base: issue-760)
3. Split changes by issue ownership:
   - #761: Reader empty-owner cleanup (10 files)
   - #760: Collector owner OID resolution (13 files)
   - #762: Regression test coverage (6 files)
4. Pushed all branches and created PRs with proper stacking

### Learnings

- **Stash-and-split pattern**: Safe way to remediate policy violations without data loss
- **Stacked PRs**: Use when issues have dependencies; merge in order, retarget after each merge
- **Prevention**: Agents must read decisions.md before starting work — directive was already recorded twice

---

## 2026-04-20 — Sprint 21 Kickoff & Milestone Planning

**Status:** ✅ COMPLETE (Sprint Planning)

### Sprint 21 — Collector Owner OID Completeness

Assigned 3 issues to Sprint 21 (focused on Round 1 #609 gaps):
- #760: Source collector owner OID from collector records (squad:trinity)
- #761: Remove empty-owner reader scaffolding (squad:trinity) — moved from Sprint 20
- #762: Add regression coverage for collector owner threading (squad:tank)

### Scope-to-Role Migration Scheduling

Created 3 new milestones and assigned 7 issues across future sprints:

| Sprint | Phase | Issues |
|---|---|---|
| Sprint 22 | Phase 0 | #763, #764 |
| Sprint 23 | Phases 1-2 | #765, #766 |
| Sprint 24 | Phases 3-4 | #767, #768, #769 |

### Learnings
- Milestones are the source of truth for sprint planning (not labels)
- Scope migration phases respect dependency chain: Phase N unblocks Phase N+1
- Sprint 21 stays focused on collector owner work — one coherent deliverable

---

## 2026-04-20 — Scope-to-Role Migration: GitHub Issues Created

**Status:** ✅ COMPLETE (Issue Triage & Creation)

### Issues Created (7 total, dependency-ordered)

| # | Phase | Title | Labels | Squad |
|---|---|---|---|---|
| #763 | 0 | Extract EntraClaimsTransformation to shared project | scope-removed-phase-0 | neo, trinity |
| #764 | 0 | Add role-based authorization policies to API Program.cs | scope-removed-phase-0 | trinity |
| #765 | 1 | Replace 37 scope checks with role-based policies in API controllers | scope-removed-phase-1 | trinity |
| #766 | 2 | Migrate 90+ API test scope references to role-based claims | scope-removed-phase-2 | tank |
| #767 | 3 | Remove scope constants from Domain and simplify API + Web config | scope-removed-phase-3 | trinity, switch |
| #768 | 4 | Remove 42 custom delegated scopes from Azure Entra app registration | scope-removed-phase-4 | Joe |
| #769 | 4 | Azure Portal cleanup: remove stale scope-related configuration | scope-removed-phase-4 | Joe |

### Labels Created
- `scope-removed-phase-0` through `scope-removed-phase-4`

### Learnings
- Phase labels (`scope-removed-phase-N`) provide clear sequencing for multi-phase migrations
- Portal/ops work (#768, #769) labeled `squad:Joe` since it requires Azure Portal access
- Phase 3 kept as single issue despite touching Domain/API/Web — changes are atomic (removing Scopes.cs breaks dependents simultaneously)
- Dependency chain: #763 → #764 → #765 → #766 → #767 → #768 → #769

---

## 2026-04-20 — Scope-to-Role Migration Plan

**Status:** ✅ COMPLETE (Read-Only Planning)  
**Artifact:** `.squad/decisions/inbox/neo-scope-to-role-migration.md`

### Findings

- **37 `VerifyUserHasAnyAcceptedScope` call sites** across 5 API controllers (Engagements: 18, Schedules: 10, SocialMediaPlatforms: 5, UserPublisherSettings: 4, MessageTemplates: 3)
- **90+ test references** to `Domain.Scopes.*` in API test files
- **36 scopes** configured in Web `appsettings.Development.json` under `DownstreamApis:JosephGuadagnoBroadcastingApi:Scopes`
- Web layer already has battle-tested role model: `EntraClaimsTransformation` + 4 authorization policies
- `GetOwnerOid()` / `IsSiteAdministrator()` are scope-independent — unaffected by migration
- `XmlDocumentTransformer` and Scalar config both enumerate all 42 scopes for OpenAPI docs

### Architecture Decision

5-phase migration: (0) Add role infra to API, (1) Replace scope checks with role checks, (2) Update tests, (3) Remove scope constants/config, (4) Clean Entra app registration. Key decision point: extract `EntraClaimsTransformation` to shared project since it only depends on `IUserApprovalManager`.

### Key Files (Migration Surface)

- `src\JosephGuadagno.Broadcasting.Domain\Scopes.cs` — 42 scope constants to remove (keep MicrosoftGraph)
- `src\JosephGuadagno.Broadcasting.Api\Program.cs` — Needs `IClaimsTransformation` + `AddAuthorization`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\*.cs` — 37 scope check call sites
- `src\JosephGuadagno.Broadcasting.Api\XmlDocumentTransformer.cs` — Scope enumeration in OpenAPI
- `src\JosephGuadagno.Broadcasting.Api.Tests\Helpers\ApiControllerTestHelpers.cs` — Scope claim setup
- `src\JosephGuadagno.Broadcasting.Web\appsettings.Development.json` — 36 API scope entries
- `src\JosephGuadagno.Broadcasting.Web\EntraClaimsTransformation.cs` — Model to replicate/share

---

## 2026-04-20 — Auth Architecture Review: Entra Scope Limits with Personal Accounts

**Status:** ✅ COMPLETE (Read-Only Analysis)  
**Artifact:** `.squad/decisions.md` (merged from inbox)

### Findings

- App defines **42 custom delegated scopes** in `Scopes.cs`; Web requests **37** at consent time
- Microsoft Entra has practical scope limits for personal (MSA) accounts — this is the blocker
- Web layer **already uses app-managed roles** (SQL-backed, via `EntraClaimsTransformation`) — the role model is battle-tested
- API layer enforces authorization via `VerifyUserHasAnyAcceptedScope(...)` — this is the only place scopes are consumed for authZ
- Ownership enforcement (`GetOwnerOid()`, `IsSiteAdministrator()`) is scope-independent and stays intact

### Recommendation

**Option B: Keep Entra for authentication (token issuance + validation), move authorization to app-managed roles in the API.** Replace scope-based checks with role-based policies matching the Web layer pattern. Collapse Entra scopes to 1-2 (audience + `User.Read`).

### Key Files

- `src\JosephGuadagno.Broadcasting.Domain\Scopes.cs` — 42 scope constants (to be reduced)
- `src\JosephGuadagno.Broadcasting.Web\EntraClaimsTransformation.cs` — Role injection from SQL (model to replicate in API)
- `src\JosephGuadagno.Broadcasting.Web\Program.cs` lines 103-111 — Scope union + downstream API registration
- `src\JosephGuadagno.Broadcasting.Web\appsettings.Development.json` — 36 API scopes configured
- `src\JosephGuadagno.Broadcasting.Api\Controllers\*.cs` — All `VerifyUserHasAnyAcceptedScope` call sites (to be replaced)
- `src\JosephGuadagno.Broadcasting.Domain\Constants\RoleNames.cs` — Existing role hierarchy

---

## 2026-04-19 — Architectural Analysis: #731 Settings Design (Read-Only)

**Status:** ✅ COMPLETE  
**Issue:** #731 (Per-user publisher settings)  
**Artifact:** `.squad/agents/neo/analysis-731-settings-design.md`

### Findings

**Question:** Why flat key/value dictionary instead of serializing provider-specific objects?

**Answer:** Flat key/value with strongly-typed in-memory bridge is the **correct choice**:

1. **Write-only secret masking** is trivial with flat dict; nested objects require post-processing
2. **Partial updates** are safe: `MergeSecret()` keeps existing values if incoming is null
3. **Platform extensibility** requires no EF migrations or recompilation
4. **API contract stability** is achieved via typed DTOs; secrets never exposed in raw form

The implementation **satisfies the original JSON-blob intent** while making smarter architectural choices around secret handling, update safety, and extensibility. The flat structure is both more maintainable and more secure than direct object serialization.

### Learnings

- **Pattern:** Persistence layer stores flat data (flexible, extensible); business logic validates and builds typed objects; API layer masks secrets in responses
- **Future evolution:** If structured storage is later needed, migration path exists without breaking API or data
- **Key design principle:** Layers have clear responsibility — security is built at manager/data-store boundary, not scattered

---

## Sprint 20 Final — PR #756 & #757 Merge & Session Close (2026-04-19)

**Status:** ✅ COMPLETE  
**PRs:** #756, #757  
**Issues:** #731, #732

### Work Summary

Final review and merge cycle for Sprint 20 completion:
- PR #756 (feat(#731): add per-user publisher settings) — Recovered from `neo/pr-recovery-731-732`, pushed to branch, reviewed, ready to merge
- PR #757 (test(#732): owner isolation coverage) — Reviewed, squad approved, regular comment posted for author merge

Both PRs passed squad review. Local cleanup completed by Link: branches deleted, main synced to `0bcc1fe`, working tree clean.

### Decisions Recorded

Three sprint wrap decisions merged into `.squad/decisions.md`:
1. **link-sprint20-cleanup** — Branch deletion and main sync details
2. **neo-pr-756-push-and-comment** — Recovery strategy for #731
3. **neo-pr-757-github-comment** — Comment vs. review decision for #757

Session logs and orchestration log recorded.

## 2026-04-20 — Sprint 21 Kickoff & Milestone Planning (Updated)

**Status:** ✅ COMPLETE (Sprint Planning + Orchestration)

### Outcome Summary (Session: Sprint 21 Kickoff)
- ✅ **Milestones finalized:** Sprint 21 (3 issues), Sprint 22-24 (7 issues across phases)
- ✅ **Trinity deliverable:** Collector owner threading implementation (#760, #761)
- ✅ **Tank deliverable:** Regression coverage for #762 with fail-closed + happy-path tests
- ✅ **Bootstrap blocker:** data-seed.sql needs owner-bearing source records alignment

### Orchestration Log
- Generated: .squad/orchestration-log/2026-04-20T18-39-46Z-neo.md
- Session Log: .squad/log/2026-04-20T18-39-46Z-sprint-21-kickoff.md
- Decisions merged: 4 inbox files → decisions.md (neo-sprint21-milestone-plan, trinity-collector-owner-bootstrap-blocker, tank-762-regression-coverage, copilot-directive)

### Next Phase
- Sprint 21 execution: Trinity (#760, #761), Tank (#762), Neo review support
- Monitor Trinity merges for regression test suite compliance
- Bootstrap blocker: Track data-seed.sql alignment before final Sprint 21 close

## Learnings

### 2026-04-20 — Neo PR Comment Template (Canonical Standard)

- **Pattern**: Two-mode comment structure ensures consistency across Neo reviews
  - Formal Review: comprehensive, audit-trail quality, for multi-finding PRs
  - Quick Finding: minimal, targeted, for single blockers or guidance
- **Template location**: `.squad/skills/neo-pr-comment/TEMPLATE.md`
- **Decision framework**: `.squad/skills/neo-pr-comment/DECISION-TREE.md`
- **Key principle**: Formal reviews use checklist + subsystem breakdown; verdict at end (APPROVED/BLOCKED/NEEDS REVISION)
- **Production anchors**: PR #736 (Formal Review pattern), PR #771 (Quick Finding pattern)
- **Posting**: Always use PowerShell `gh api` on Windows — produces visible comment (required for squad protocol)
- **Directive enforcement**: Violations are BLOCKING (not "could be improved"); findings are always actionable

### 2026-04-20 — Visible PR comment workflow for stacked reviews
- For author-owned PRs, Neo should post a regular PR comment instead of a formal review so the finding is visible without using an approval artifact the author cannot meaningfully self-consume.
- Current Sprint 21 stack status: #770 merged first; #771 is blocked by `scripts\database\data-seed.sql` lacking bootstrap `CreatedByEntraOid` values for collector source rows; #772 is blocked by unrelated drift in `src\JosephGuadagno.Broadcasting.Web\appsettings.Development.json`.
- For stacked PRs, after each upstream merge the next PR should be retargeted to `main` and revalidated before clearing it.

### 2026-04-20 — Open PR Review (#770, #771, #772)
- Stacked PR review gate: each PR must build and test against its current base; a downstream PR cannot be the fix for an upstream branch break.
- PR #771 (`issue-760`) currently removes `Settings.OwnerEntraOid` in `src\JosephGuadagno.Broadcasting.Functions\Models\Settings.cs` and `src\JosephGuadagno.Broadcasting.Functions\Interfaces\ISettings.cs`, but its branch still fails in `src\JosephGuadagno.Broadcasting.Functions.Tests\Collectors\LoadAllPostsTests.cs`, `LoadAllVideosTests.cs`, `LoadNewPostsTests.cs`, `LoadNewVideosTests.cs`, and `src\JosephGuadagno.Broadcasting.Functions.Tests\Startup.cs` because those tests still reference the deleted scaffold.
- Fresh-environment bootstrap is still blocked independently of the PR stack: `scripts\database\data-seed.sql` seeds `SyndicationFeedSources` without `CreatedByEntraOid`, so the new fail-closed owner resolution cannot resolve an owner on a clean database until SQL seed data is aligned.
- PR #772 (`issue-762`) validated green locally once stacked (`dotnet build .\src\ --no-restore --configuration Release` and CI-aligned `dotnet test`), but it also carries unrelated Web config drift in `src\JosephGuadagno.Broadcasting.Web\appsettings.Development.json`; that kind of cross-issue payload is a blocking review defect under the one-PR-per-issue rule.

### 2026-04-20 — Local + CI PR guardrail package
- Enforce the same single-issue rule in both places: local hooks for fast feedback, CI metadata validation for merge protection.
- Branch names now standardize on `issue-<number>-<slug>` or `feature/<number>-<slug>` so the issue number is machine-readable.
- PR titles must use `<type>(#<issue>): <summary>` and the PR body must carry exactly one matching closing issue reference.

---

## 2026-04-21 — PR #801 Revision: API RBAC Phase 0 Restored (Awaiting Re-Review)

**Status:** ✅ COMPLETE (Trinity Implementation + Validation) | ⏳ PENDING (Neo Re-Review)  
**Issue:** #764 (API RBAC Phase 0)  
**Branch:** `issue-764-api-rbac`  
**Dependency:** PR #800 merged

### Context

PR #801 had been rejected because it lost its product implementation and only preserved a skill artifact. The acceptance criteria for issue #764 required a full Phase 0 MVP: API-side claims transformation registration, role policy wiring, and infrastructure-layer test seams.

### Trinity's Restoration

**Commit:** 68c75aa  
**Components Restored:**

1. **API auth registration extension** (`AddBroadcastingApiAuthorization()`)
   - Thin DI wrapper in `src\.../Api/Authentication/ServiceCollectionExtensions.cs`
   - Registers `EntraClaimsTransformation` (now from Managers via PR #800 merge)
   - Keeps `Program.cs` clean

2. **Four hierarchical role policies**
   - `IsOwner`, `IsModerator`, `IsMember`, `IsGuest`
   - Matches Web layer pattern
   - Ready for Phase 1 scope-to-role controller migration

3. **Infrastructure + policy tests**
   - `ApiAuthenticationTests.cs` — Validates DI registration and claims transformation
   - `ApiAuthorizationPoliciesTests.cs` — Validates policy enforcement
   - Public test helpers for reuse

4. **Test helper unification**
   - Cleaned `SocialMediaPlatformsControllerTests` to reuse `ApiControllerTestHelpers`
   - Reduced duplication across test projects

5. **PR metadata fixes**
   - Corrected base branch, target milestone
   - PR #801 now properly positioned in Sprint 22 Phase 0

### Phase 0 Design Rationale

**Intentionally additive, not disruptive:**
- Controller-level `VerifyUserHasAnyAcceptedScope(...)` checks **remain in place** for deliberate dual enforcement
- No controller policy rewrites in Phase 0
- API gains the host foundation (`EntraClaimsTransformation`, role policies) without changing authorization behavior
- Later Phase 1-4 will migrate controllers to role-based checks

### Validation

- ✅ `dotnet restore .\src\`
- ✅ `dotnet build .\src\ --no-restore --configuration Release`
- ✅ `dotnet test .\src\ --no-build --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader"` — All tests pass

### Awaiting

Neo re-review on PR #801 for approval and merge clearance.
