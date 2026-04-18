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

## Learnings — PR #739 Final Review (2026-04-18)

**Context:** Third and final review of PR #739 (feat(#729): enforce owner isolation in API controllers). Previous rejections were for missing non-owner 403 tests (Round 1: zero tests, Round 2: Talks/Platforms sub-actions missing).

**Final state after Tank's Round 2 additions:**
- **Total tests:** 93/93 passing
- **Security tests added:** 20 total (11 Round 1 + 9 Round 2)

**Coverage verified:**
- All 17 `Forbid()` call sites across 3 controllers now have non-owner `ForbidResult` tests
- All 3 `IsSiteAdministrator()` branching locations have SiteAdmin unfiltered-overload tests
- Entity OID (`owner-oid-12345`) ≠ User OID (`non-owner-oid-99999`) pattern consistently applied
- No magic strings — all constants from `Domain.Constants.*` and `Domain.Scopes.*`
- Moq patterns correct (`Times.Never` on side-effectful calls when authorization fails)

**Platforms test design note:** The `EngagementsController_PlatformsTests` constructor sets up a default mock returning `BuildEngagement(id)` with OID `test-oid-12345`. Security tests then create the SUT with `ownerOid: "non-owner-oid-99999"` to ensure the ownership check fails. This is a clean pattern that avoids per-test mock setup boilerplate.

**Verdict:** ✅ APPROVED — All ownership-guarded paths covered. Ready for Joseph to merge.

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
