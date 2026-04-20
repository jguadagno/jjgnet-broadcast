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

# Neo - History

## PR #738 Review — Web MVC Ownership Enforcement (2026-04-18)

**Context:** First review of PR #738 (feat(#730): enforce owner isolation in Web MVC controllers). This is the companion PR to #739 (API ownership) which was merged earlier today.

**Branch state issue:** The `issue-730` branch was created from a local state that included API changes before PR #739 was merged. Now that #739 is on main, there are merge conflicts in the API test files.

**Conflicts detected:**
- `src/JosephGuadagno.Broadcasting.Api.Tests/Controllers/EngagementsControllerTests.cs`
- `src/JosephGuadagno.Broadcasting.Api.Tests/Controllers/EngagementsController_PlatformsTests.cs`
- `src/JosephGuadagno.Broadcasting.Api.Tests/Controllers/SchedulesControllerTests.cs`

**Web MVC implementation review (the actual PR content):**

✅ **Correct pattern applied:**
- Uses `User.FindFirstValue(ApplicationClaimTypes.EntraObjectId)` for current user
- Uses `RoleNames.SiteAdministrator` constant for admin bypass (not `Administrator`)
- Returns friendly redirect + `TempData["ErrorMessage"]` instead of raw `Forbid()`
- Proper layering: controllers → services → managers (no direct data store access)

✅ **Controllers covered:**
- EngagementsController: Details, Edit (GET), Delete (GET), DeleteConfirmed, Add (sets OID)
- SchedulesController: Details, Edit (GET), Delete (GET), DeleteConfirmed, Add (sets OID)
- TalksController: Details, Edit (GET), Delete (GET), DeleteConfirmed, Add (sets OID)
- MessageTemplatesController: Edit (GET)

✅ **Tests updated:** Web tests correctly set up user claims with matching `CreatedByEntraOid`

✅ **CI passing:** All 4 checks green (CodeQL, build-and-test, GitGuardian, CodeQL Analysis)

**Verdict:** ❌ CHANGES REQUESTED — Rebase required to resolve API test conflicts. Once rebased, the Web MVC implementation is correct and ready to merge.

**Action posted:** PR comment #738 with detailed review and rebase instructions.

---

## 2026-04-18 — PR #739: Final Review (Round 3) — APPROVED & MERGED

**Status:** ✅ COMPLETE  
**PR:** #739 (feat(#729): enforce owner isolation in API controllers)  
**Issue:** #729

### Work Summary

Conducted final (third) review of PR #739 after two previous rejection rounds. All 17 Forbid() call sites now have comprehensive non-owner ForbidResult tests. Total security test suite: 20 tests across three rounds (11 Round 1 + 9 Round 2 added by Tank).

### Coverage Verified
- All 17 `Forbid()` call sites across 3 controllers (EngagementsController, TalksController, PlatformsController) have non-owner ForbidResult tests
- All 3 `IsSiteAdministrator()` branching locations verified
- Entity OID (`owner-oid-12345`) ≠ user OID (`non-owner-oid-99999`) pattern applied consistently
- No magic strings — all constants from Domain.Constants and Domain.Scopes
- Moq patterns correct (`Times.Never` on side-effectful calls)

### Test Results
- **Total:** 93/93 passing
- **Security tests:** 20 total
- **Pattern:** EngagementsController (6), TalksController (4), PlatformsController (4), SiteAdmin overloads (6)

### Decision
✅ **APPROVED** — Ready for merge. All ownership-guarded paths covered.

### Outcome
Joseph merged PR #739 to main. Security test coverage complete.

### Related
- **Round 1:** Zero tests → rejected
- **Round 2:** 9 missing Talks/Platforms tests → Tank added coverage → rejected (but closer)
- **Round 3:** Final verification → approved and merged

---

## 2026-04-18 — Session: Neo Setup Experience Spec & Tank Test Fixes

**Status:** ✅ COMPLETE (Background Agent)  
**Focus:** Architecture spec for multi-user setup experience (issue #609)

### Work Summary

Produced comprehensive architecture specification for new user setup experience — the wizard that runs after a user is approved and before they access the main application.

**Deliverable:** `setup-experience-spec.md` (90 pages) + architectural decisions document

### Key Deliverables

1. **Feature Spec**
   - Problem statement: approved users have no path to configure personal collectors/publishers
   - 8-step user flow: approval → setup welcome → collectors → publishers → review → complete
   - UI requirements (YouTube, SyndicationFeed collectors; Bluesky, Twitter, LinkedIn, Facebook publishers)
   - Database schema (UserCollectorSettings JSON blob, HasCompletedSetup flag on ApplicationUsers)
   - Middleware placement (after approval gate, before authorization)

2. **7 Architectural Decisions**
   - JSON blob storage (consistency with #731 UserPublisherSettings)
   - Setup middleware placement (after approval, before auth)
   - HasCompletedSetup boolean column on ApplicationUsers
   - Data Protection API encryption (MVP), Key Vault (future)
   - Soft redirect + skip option + persistent banner enforcement
   - Direct credentials (MVP), OAuth (future)
   - Named type constants + SQL CHECK constraints

3. **3 Open Questions (Team Feedback Incorporated)**
   - Test connection buttons: Yes (recommendation)
   - Partial config UX: validation error (recommendation)
   - Re-enterable setup: Yes, via Settings page (recommendation)

### Related Issues

- Epic #609: Multi-tenancy — per-user content, publishers, and social tokens
- Issue #731: Per-user publisher settings
- Sprint 15 (pending prioritization)

### Decision Document

All architectural choices documented in decisions.md with full context and rationale.

---

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
- ScheduledItems.Platform -> SocialMediaPlatformId int FK; MessageTemplates.Platform -> SocialMediaPlatformId
- Seed: Twitter(1), BlueSky(2), LinkedIn(3), Facebook(4), Mastodon(5); Talks inherit from parent Engagement
- Sprint 1 DB+EF (Morpheus), Sprint 2 API+Manager (Trinity), Sprint 3 Web+Tests (Switch/Sparks/Tank/Neo)

**IaC (Bicep):** Circular dependency: never Module A->B where B->A; listKeys() exposes secrets (use managed identity); StorageV2; ConnectionString over InstrumentationKey; Pin all API versions to GA (no -preview); allowBlobPublicAccess:false; event-grid.bicep is in modules/data/ not monitoring/

**Completed:** RBAC Phase 1&2, Email, Bicep IaC, Technical Debt, Junction table, Epic #667

**Team standing rules:** Only Joseph merges PRs; All mapping via AutoMapper; Paging at data layer only

---

*See history-archive.md for prior work.*

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

### 2026-04-20 — Visible PR comment workflow for stacked reviews
- For author-owned PRs, Neo should post a regular PR comment instead of a formal review so the finding is visible without using an approval artifact the author cannot meaningfully self-consume.
- Current Sprint 21 stack status: #770 merged first; #771 is blocked by `scripts\database\data-seed.sql` lacking bootstrap `CreatedByEntraOid` values for collector source rows; #772 is blocked by unrelated drift in `src\JosephGuadagno.Broadcasting.Web\appsettings.Development.json`.
- For stacked PRs, after each upstream merge the next PR should be retargeted to `main` and revalidated before clearing it.

### 2026-04-20 — Open PR Review (#770, #771, #772)
- Stacked PR review gate: each PR must build and test against its current base; a downstream PR cannot be the fix for an upstream branch break.
- PR #771 (`issue-760`) currently removes `Settings.OwnerEntraOid` in `src\JosephGuadagno.Broadcasting.Functions\Models\Settings.cs` and `src\JosephGuadagno.Broadcasting.Functions\Interfaces\ISettings.cs`, but its branch still fails in `src\JosephGuadagno.Broadcasting.Functions.Tests\Collectors\LoadAllPostsTests.cs`, `LoadAllVideosTests.cs`, `LoadNewPostsTests.cs`, `LoadNewVideosTests.cs`, and `src\JosephGuadagno.Broadcasting.Functions.Tests\Startup.cs` because those tests still reference the deleted scaffold.
- Fresh-environment bootstrap is still blocked independently of the PR stack: `scripts\database\data-seed.sql` seeds `SyndicationFeedSources` without `CreatedByEntraOid`, so the new fail-closed owner resolution cannot resolve an owner on a clean database until SQL seed data is aligned.
- PR #772 (`issue-762`) validated green locally once stacked (`dotnet build .\src\ --no-restore --configuration Release` and CI-aligned `dotnet test`), but it also carries unrelated Web config drift in `src\JosephGuadagno.Broadcasting.Web\appsettings.Development.json`; that kind of cross-issue payload is a blocking review defect under the one-PR-per-issue rule.
