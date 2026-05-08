## Summary

Neo (Reviewer/Architect) serves as the technical authority for API design, architectural decisions, and cross-agent coordination. Key responsibilities include API endpoint standardization, versioning strategy, DTO patterns, and RBAC implementation. Neo has established patterns for response shapes, error handling, and security policies across the API layer. Major contributions include designing the API versioning and DTO strategy, implementing role-based authorization architecture, enforcing ownership isolation principles, and providing architectural guidance to Backend (Trinity), Frontend (Switch), Testing (Tank), and Polish (Sparks) agents. Neo coordinates design reviews, resolves architectural conflicts, and ensures consistency across layers. Key decision artifacts: API versioning specification, DTO/response mapping patterns, RBAC authorization design, and ownership enforcement principles. Neo's work directly influences how Trinity implements backends, how Tank writes tests, how Switch builds Web-layer services, and how Sparks integrates UI features. Pattern: Neo proposes, coordinates feedback, documents decisions in `.squad/decisions/`, and provides code examples for other agents to follow. Notable: Neo has maintained architectural consistency despite rapid feature development and maintained security boundaries across all layers.

## 2026-05-08 — Issue #803: .squad/ Housekeeping PR Policy

**Status:** ✅ COMPLETE — PR #934 opened, comment posted on #803

**Action:** Implemented Option B — added a `.squad/`-only path-detection bypass to the `pr-metadata` CI job in `.github/workflows/ci.yml`. When all changed files in a PR are under `.squad/`, branch naming, PR title format, and issue-linking checks are skipped. Decision written to `.squad/decisions/inbox/neo-803-squad-commit-policy.md`.

**Branch protection finding:** `gh api repos/jguadagno/jjgnet-broadcast/branches/main/protection` returns 404 — no GitHub-native branch protection rules are active. All enforcement is CI-only via `ci.yml`.

**Learning:** Pushing workflow file changes requires `workflow` OAuth scope. HTTPS remotes without that scope are rejected. Use SSH remote (`git remote set-url origin git@github.com:...`) as the workaround when the token lacks `workflow` scope.

## 2026-05-01 — Release Build Warnings: GitHub Issues Created

**Status:** ✅ COMPLETE — Issues #903–#905 created from warning triage

**Action:** Created three GitHub issues to track and fix build warning categories identified in the release build triage (`.squad/decisions/inbox/neo-build-warnings-triage.md`).

**Issues created:**
- #903: `fix: upgrade Newtonsoft.Json to 13.0.3+ to resolve NU1903 CVE` (security, bug)
  - Covers NU1903 vulnerability in Newtonsoft.Json 10.0.2 across 13 projects
  - 22 warnings, CRITICAL severity, high-severity CVE GHSA-5crp-9r3c-p9vr
- #904: `refactor: resolve CS8xxx nullable reference warnings across solution` (enhancement, refactor)
  - Covers 115 nullable reference type warnings across 5 projects
  - CS8618 (50), CS8625 (29), others (36)
  - Domain (32), Web (12), Managers.LinkedIn (8), Data.Sql (5), Data (1)
- #905: `chore: suppress xUnit1051 and NETSDK1206 build noise` (enhancement)
  - Covers xUnit1051 (290 warnings, test hygiene), NETSDK1206 (37 warnings, vendor library), NU1510 (4 warnings, unnecessary packages)

---

## 2026-04-30 — Issue Epic Scoping: Social Media Message Composition Refactor

**Status:** ✅ COMPLETE — Issues #899–#902 created; #69 comment posted

**Action:** Confirmed #69 (customize social messages with Sciban templates) as the parent epic. #45 and #46 are related detail issues already in scope. Created four platform-specific refactor issues to move message composition (Sciban template rendering) from Azure Functions into Manager layer.

**Issues created:**
- #899: refactor: move Twitter message composition to TwitterManager
- #900: refactor: move Facebook message composition to FacebookManager
- #901: refactor: move Bluesky message composition to BlueskyManager
- #902: refactor: move LinkedIn message composition to LinkedInManager

**Comment posted to #69:** https://github.com/jguadagno/jjgnet-broadcast/issues/69#issuecomment-4349212612

## Learnings
- **Sprint milestone structure (2026-04-30)**: Created Sprint 29 and Sprint 30 milestones. Sprint N starts at milestone number 23 + N - 23. Keep sprints focused on thematic coherence (e.g., OAuth token fixes vs. publisher refactor). Quick-hitting fixes go first; architectural work (ISocialMediaPublisher, message composition consolidation) follows naturally.

**Backlog review & next-up planning (2026-05-01).** Reviewed open backlog (23 issues). Sprint 28 complete (#852 + #853 merged, #856 pending Joe manual step). Next sequence is #897–#902 (ISocialMediaPublisher refactor + message composition moves to Managers). Recommended 3-5 item priority stack: (1) #893 + #890 quick wins, (2) #897 ISocialMediaPublisher interface (blocker for #902–#899), (3) #902–#899 composition refactor in order, (4) #892 + #896 Joe parallel tasks. #724 deferred until #609 per-user isolation confirmed complete. Team assignments: Trinity (#893, #890), Tank (#897, #902–#899), Joe (#856 async, #892, #896).

**Release build warnings triage completed (2026-05-01).** Full Release build produces 1067 warning lines across 6 categories:
1. **CS8xxx (Nullable):** 115 warnings — CS8618 (non-nullable property) dominates at 50 occurrences, followed by CS8625 (null literal) at 29. Heaviest in Domain (32), Web (12), Managers.LinkedIn (8), Data.Sql (5).
2. **NU1903 (Vulnerable packages):** 22 projects reference Newtonsoft.Json 10.0.2 with GHSA-5crp-9r3c-p9vr high severity vulnerability. 
3. **NU1510 (Unnecessary packages):** 4 projects carry prunable package references (Managers, Web, Api).
4. **xUnit1051 (CancellationToken):** 290 test warnings suggesting `TestContext.Current.CancellationToken` usage.
5. **NETSDK1206 (RID warnings):** 37 warnings about win7-x64 RID in Microsoft.Azure.DocumentDB.Core (legacy .NET 8 deprecation notice).
6. **Build metadata noise:** ~400 lines of "EnableIntermediateOutputPathMismatchWarning" status and "succeeded with X warning(s)" summaries.

**Critical findings:** NU1903 is the only security-critical category (high severity vulnerability). CS8xxx nullable warnings are correctness issues but not runtime blockers (nullable reference types are compile-time only). xUnit1051 is test hygiene (non-blocking). NU1510 and NETSDK1206 are informational.

**Recommendation:** Fix NU1903 first (upgrade Newtonsoft.Json → 13.0.3+). Nullable warnings are a batch-fix candidate (2-4 hours, one PR). xUnit warnings can be fixed or suppressed project-wide. NU1510 should be investigated (may legitimately need those packages). NETSDK1206 is library vendor issue, suppress via NoWarn.

**Architecture decision: Composition in Managers, Publish-Only in Functions.** Message composition logic (Sciban template selection and rendering) belongs in the Manager layer; Azure Functions should be responsible only for the publish/send call. This establishes a clean separation of concerns:
- **Managers:** Business logic and message composition
- **Functions:** Execution of publish/send operations to platform APIs
- **Tests:** Composition can be tested independently from publish mechanics

The codebase currently has all four social media platforms (Twitter, Facebook, Bluesky, LinkedIn) with dedicated Functions folders and no dedicated Manager classes. The refactor distributes composition upward into managers, keeping Functions lean.

---

## 2026-04-28 — PR #889 Review: LinkedIn OAuth Token Expiry Data Layer

**Status:** ✅ APPROVED — PR #889, Issue #852

**What was reviewed:** Trinity's implementation of `LastNotifiedAt` column and expiry window methods for `UserOAuthTokens`.

**Review result:** APPROVED — no blocking issues. Comment posted at https://github.com/jguadagno/jjgnet-broadcast/pull/889#issuecomment-4335350588

## Learnings
- **Sprint milestone structure (2026-04-30)**: Created Sprint 29 and Sprint 30 milestones. Sprint N starts at milestone number 23 + N - 23. Keep sprints focused on thematic coherence (e.g., OAuth token fixes vs. publisher refactor). Quick-hitting fixes go first; architectural work (ISocialMediaPublisher, message composition consolidation) follows naturally.

**Issue #9 (Publisher abstraction) — PARTIALLY DONE as of 2025.** "Publisher" terminology is broadly adopted: `UserPublisherSetting*`, `EventPublisher`, `Publishers/` Functions folder. Platform managers exist as separate projects (Managers.Twitter, Managers.Bluesky, Managers.LinkedIn, Managers.Facebook) with their own platform-specific interfaces (`ITwitterManager`, `IBlueskyManager`, `ILinkedInManager`). BUT there is NO common `IPublisher`/`ISocialMediaPublisher` interface that all platform managers implement — the "pluggable" contract requested in issue #9 is absent. The `SocialMediaPlatform` model and `ISocialMediaPlatformManager` are data-management concerns, not the per-platform posting abstraction the issue asked for.

**Double input-guard pattern is acceptable.** `UpdateLastNotifiedAtAsync` validates `ownerOid` and `platformId` in both the manager and the data store. This is defense-in-depth, not a violation. Don't flag it as redundant — the data store must be self-defending.

**`GetExpiringWindowAsync` without `from <= to` guard is acceptable for scheduled Function callers.** When the only call site is a scheduled Function with a fixed, programmatically computed window, a parameter-order guard adds noise. Only add it if the method is ever exposed to external or user-controlled input.

**5-test pattern for data store methods covering a new column is the right granularity:** boundary-in, boundary-out (both ends), interior, empty window, user isolation, and not-found return value. Trinity's test set hits all required axes for this type of feature.

---

## 2026-05-01 — Performance Investigation: All Index Pages

**Status:** ✅ COMPLETE — Findings written to `.squad/decisions/inbox/neo-perf-investigation-findings.md`

## Learnings
- **Sprint milestone structure (2026-04-30)**: Created Sprint 29 and Sprint 30 milestones. Sprint N starts at milestone number 23 + N - 23. Keep sprints focused on thematic coherence (e.g., OAuth token fixes vs. publisher refactor). Quick-hitting fixes go first; architectural work (ISocialMediaPublisher, message composition consolidation) follows naturally.

**N+1 SourceTags queries are the #1 performance killer.** Both `SyndicationFeedSourceDataStore` and `YouTubeSourceDataStore` use a `foreach` loop post-materialization to load SourceTags — 27+ sequential DB roundtrips per Index page. The fix is a batch IN-clause query followed by in-memory grouping.

**Schedules/Index makes 2 sequential HTTP calls** (items list + orphan count). These are not parallelized. Fix with `Task.WhenAll`.

**Missing AsNoTracking on read-only list queries** affects Engagements, SyndicationFeedSources, YouTubeSources, ScheduledItems paged data stores. MessageTemplateDataStore is the correct reference implementation.

**SocialMediaPlatformManager paged GetAllAsync bypasses IMemoryCache.** The cached path is non-paged; the Index page uses the paged overload that hits the DB every time. Since the platform list is tiny, the correct fix is to serve paged/filtered results from the cached full list.

**MessageTemplatesController hard-codes pageSize=100 and filters in memory.** Platform filter never reaches SQL.

**Missing DB indexes on sort columns:** Engagements (StartDateTime, EndDateTime, CreatedByEntraOid), SyndicationFeedSources (Title, Author, PublicationDate, AddedOn, CreatedByEntraOid), YouTubeSources (same), ScheduledItems (SendOnDateTime standalone, CreatedByEntraOid), SocialMediaPlatforms (IsActive+Name composite).

**Managers are pass-throughs except SocialMediaPlatformManager.** No caching in SyndicationFeedSourceManager or YouTubeSourceManager — the SocialMediaPlatformManager caching pattern should be replicated for short-TTL (60s) list caching.

**`LIKE '%...%'` queries cannot use B-tree indexes.** Text `Contains` filters hit full-table scans. Not fixable without Full-Text Search. Current data volumes keep this acceptable.

---

## 2026-05-08 — Issue Triage: #930, #933, #78

**Status:** ✅ COMPLETE — Triaged three GitHub issues, posted findings, updated labels.

### Issue #930: NuGet package deprecation (squad:trinity)
✅ **Correct assignment.** `AspNetCore.HealthChecks.AzureStorage` (v7.0.0) in `ServiceDefaults.csproj` must be replaced with three packages:
- `AspNetCore.HealthChecks.Azure.Storage.Blobs`
- `AspNetCore.HealthChecks.Azure.Storage.Queues`
- `AspNetCore.HealthChecks.Azure.Data.Tables` (already present in csproj, v9.0.0)

**Scope:** ServiceDefaults project (shared by Api, Web, Functions). Minor scope clarification: verify which services *actually use* blob, queue, or table health checks. Table is already registered; blobs/queues may not be needed by all services.

### Issue #933: Azure Functions for speaking engagements (squad:cypher → squad:trinity)
🚨 **Label reassignment executed.** Cypher owns CI/CD, not Azure Functions business logic. Trinity owns this.

**Pattern confirmed:** Functions project structure — each platform folder (`Bluesky/`, `Twitter/`, `Facebook/`, `LinkedIn/`) contains `Process*.cs` files:
- `ProcessNewSyndicationDataFired.cs` (blog posts)
- `ProcessNewYouTubeDataFired.cs` (YouTube videos)
- `ProcessScheduledItemFired.cs` (scheduled items)
- `ProcessNewRandomPost.cs` (random post timer)

For speaking engagements, need 4 new functions, one per platform, with names added to `ConfigurationFunctionNames` constant class.

**Issue completeness:** 70% complete. Missing:
1. What event fires the engagement creation? (Event Grid event type/topic)
2. Message composition location — in-Function or in-Manager? (Recent refactor #899–#902 suggests Managers)
3. Owner isolation — per-user or system-wide?

### Issue #78: Add caching to WebApi (empty issue body)
**Research completed.** No API controllers currently cache except `SocialMediaPlatformsController`.

**Caching pattern confirmed:** `IMemoryCache` with 5-minute absolute expiration. `SocialMediaPlatformManager` is the gold standard:
- Cache full list + filter/paginate in memory
- Invalidate on Add/Update/Delete via `InvalidateListCaches()` helper

**Priority endpoints to cache:**
1. **High (read-heavy, rarely-changed):** MessageTemplatesController, SocialMediaPlatformsController (already done)
2. **Medium (user-owned, frequent reads):** EngagementsController, SchedulesController, UserPublisherSettingsController — cache keyed by UserId
3. **Lower (smaller result sets):** SyndicationFeedSourcesController, YouTubeSourcesController

**Recommendation:** Scope this issue — which specific controllers? Once defined, Trinity can replicate the `SocialMediaPlatformManager` pattern across them.

---

## Learnings

- **Squad assignment patterns:** `squad:cypher` = GitHub Actions CI/CD + Azure infrastructure deployment; `squad:trinity` = API + Azure Functions business logic + persistence layer. Don't confuse Azure resource management (Cypher) with Azure Functions code (Trinity).
- **Functions folder pattern:** Each platform has a dedicated folder with standardized function naming (`ProcessNew*Fired`, `ProcessScheduled*Fired`, `ProcessRandom*`). New functions should follow this pattern exactly and register in `ConfigurationFunctionNames`.
- **Caching scope for APIs:** Most API controllers are cache-naive. The `SocialMediaPlatformManager` pattern (full-list cache + in-memory pagination) is the template. Replicate it for reference data (MessageTemplates, SocialMediaPlatforms) and user-owned data (Engagements, Schedules). Invalidation strategy: `InvalidateListCaches()` on mutation.
- **PR comment markdown in PowerShell (2026-05-08):** Never use `gh pr review --body` or `gh pr comment --body` inline in PowerShell — the shell mangles special characters and causes backslash escaping. Always write to a .md temp file, convert to JSON with Python, then post via `gh api` with `--input`. This pattern ensures clean backtick formatting, proper em-dashes, and no encoding corruption.

---

## 2026-05-08 — Malformed PR Comments Replaced on PRs #939, #940, #941

**Status:** ✅ COMPLETE — Clean comments posted; old comments minimized

**Action:** Identified and replaced malformed review comments that contained backslash-word patterns (`\ScheduledItemAsync\` instead of `` `ScheduledItemAsync` ``) and garbled UTF-8 characters (e.g., `ΓÇö` for em-dash). Used the correct workflow:
1. Wrote clean comment body to temp .md file
2. Converted to JSON with Python: `json.dumps({'body': body})`
3. Posted via `gh api repos/{owner}/{repo}/issues/{N}/comments --input file.json`
4. Cleaned up temp files immediately
5. Edited old malformed comments to just say "*(replaced — see updated review below)*"

**Comments posted:**
- PR #939: New clean comment with 3 blocking issues (null dereference, missing guard, test command fix)
- PR #940: Reposted APPROVED verdict for gold-standard caching pattern
- PR #941: New clean comment on cache invalidation issue in `SentScheduledItemAsync`

**Learning:** Always use file-based JSON workflow for PR comments instead of inline `--body` flag. PowerShell mangles Markdown inline, leading to backslash escaping issues and encoding corruption. The temp file + Python JSON + `--input` pattern prevents this entirely.

---

## 2026-04-28 — AutoMapper Mapping Audit
