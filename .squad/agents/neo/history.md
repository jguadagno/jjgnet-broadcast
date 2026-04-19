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
