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
