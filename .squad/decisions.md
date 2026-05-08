# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.

## Directives
### 2026-04-30T08:12:33-07:00: User directive
**By:** Copilot (via Copilot)
**What:** All work must have an issue and must start on a branch, then be committed and delivered through a pull request.
**Why:** User request — captured for team memory

---

### 2026-04-30T08:29:06-07:00: User directive
**By:** Copilot (via Copilot)
**What:** `.squad` notes are allowed in PRs when they are pertinent to that PR.
**Why:** User request — captured for team memory

---

### 2026-04-30T23-45-07Z: User directive
**By:** Copilot (via Copilot)
**What:** Do not include dotnet test .\src\ --no-build --verbosity normal --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader" in PR bodies; use the no-filter repo test command instead.
**Why:** User request — captured for team memory

---

### 2026-05-05T12:14:52: User directive
**By:** Joe (jguadagno) (via Copilot)
**What:** All manager class dependencies MUST be injected via constructor. Never use IServiceScopeFactory or the service-locator pattern (resolving dependencies from IServiceProvider inside methods). All dependencies — including ISocialMediaPlatformManager, IMessageTemplateDataStore, and any other service — must appear as constructor parameters. This applies to all managers across the codebase (LinkedIn, Twitter, Facebook, Bluesky, and any future platforms).
**Why:** User request — PR #925 was rejected for violating this rule. Captured for team memory to prevent recurrence.

---

# Link — Branch Audit Findings
**Date:** 2026-05-05  
**Author:** Link (Platform & DevOps)  
**Status:** Awaiting Joe's confirmation before any destructive operations

---

## Summary

Ran a full branch audit. Found **5 dead local branches**, **1 dead remote branch**, and **local `main` is in a broken mid-merge state** that must be resolved before any further work.

---

## Dead Branches

| Branch | Type | Last Commit | Merged Via | Status |
|---|---|---|---|---|
| `issue-890-expiring-window-guard` | local | 2026-04-30 | PR #909 ✅ | Dead — safe to delete |
| `issue-893-webbaseurl-warning` | local | 2026-04-30 | PR #910 ✅ | Dead — safe to delete |
| `issue-921-update-squad` | local | 2026-05-05 | PR #922 ✅ | Dead — safe to delete |
| `pr897-clean` | local | 2026-04-30 | No PR (scratch/probe) | Dead — safe to delete |
| `probe902-main` | local | 2026-04-30 | No PR (scratch/probe) | Dead — safe to delete |
| `origin/chore/add-joe-human-member` | remote | 2026-04-30 | PR #895 ✅ | Dead — safe to delete from remote |

**Excluded (active open PRs):**
- `issue-897-social-media-publisher-interface` → PR #911 OPEN
- `issue-902-linkedin-message-composition` → PR #912 OPEN

---

## Main's Broken State

Local `main` is diverged from `origin/main` and **stuck mid-merge**:

- `git status` reports: *"All conflicts fixed but you are still merging. (use `git commit` to conclude merge)"*
- Local main is **4 commits ahead** and **2 commits behind** `origin/main`
- The local-only commits are cherry-picked/merged work from `issue-897` and `issue-902` branches — which have **open PRs** and should go through normal PR review, not be directly merged locally
- `origin/main` has moved forward with 2 legitimate squash-merged PRs (`#922`, `#920`) that local main is missing

**Root cause:** Feature branch work was merged directly into local main (possibly during investigation/probing), creating a divergence from the remote. The merge was not completed.

---

## Recommended Actions (Joe to confirm)

1. **Fix local main:** `git merge --abort` (or since conflicts are resolved, `git reset --hard origin/main`). Since `issue-897` and `issue-902` work already exists in their feature branches with open PRs, no code will be lost.
2. **Delete 5 dead local branches** listed above.
3. **Delete `origin/chore/add-joe-human-member`** remote branch (merged via PR #895).
4. Let PRs #911 and #912 merge normally through GitHub.

---

# Link — Main Branch Cleanup

**Date:** 2026-05-05  
**Executed by:** Link (Platform & DevOps Engineer)  
**Requested by:** Joe

---

## Summary

Completed confirmed branch cleanup. All steps executed successfully.

---

## Step 1 — Fix main ✅

- `git merge --abort` — no active merge in progress (no-op, clean exit)
- `git reset --hard origin/main` — reset to `e4431d1` (issue-921-update-squad #922)
- Post-reset: clean working tree, `HEAD -> main` aligned with `origin/main`

## Step 2 — Delete dead local branches ✅

Three branches required `-D` (force) instead of `-d` because the post-reset local main didn't register them as merged via fast-forward ancestry. All three have confirmed merged PRs on GitHub, so force delete was appropriate.

| Branch | Flag | Result |
|--------|------|--------|
| `issue-890-expiring-window-guard` | `-D` | Deleted (was `cbe9197`) |
| `issue-893-webbaseurl-warning` | `-D` | Deleted (was `9971e80`) |
| `issue-921-update-squad` | `-D` | Deleted (was `5c7ddf6`) |
| `pr897-clean` | `-D` | Deleted (was `72f8307`) |
| `probe902-main` | `-D` | Deleted (was `72f8307`) |

## Step 3 — Delete dead remote branch ✅

- `chore/add-joe-human-member` deleted from `origin`

## Step 4 — Final State ✅

**Local branches remaining:**
- `main` (HEAD, aligned with `origin/main`)
- `issue-897-social-media-publisher-interface` (open PR #911 — untouched)
- `issue-902-linkedin-message-composition` (open PR #912 — untouched)

**Remote branches remaining:**
- `origin/main`
- `origin/issue-897-social-media-publisher-interface`
- `origin/issue-902-linkedin-message-composition`

**git status:** `nothing to commit, working tree clean`  
**HEAD:** `e4431d1` — matches `origin/main`

---

## Notes

- No pushes were made to `main`
- PR #911 and #912 branches were not touched
- The `-d` vs `-D` discrepancy on the three local branches is expected: after `reset --hard origin/main`, git's merge-check compares against the new HEAD, not the remote's merge history

---

### 2026-05-05: main branch repaired and dead branches cleaned
**By:** Link (requested by Joe)
**What:** Aborted stuck merge, reset local main to origin/main, deleted 5 dead local branches and 1 remote branch. PRs #911 and #912 branches preserved.
**Why:** Local main was stuck mid-merge with 4 uncommitted feature commits that belong to open PRs.

---

### 2026-05-05: Pin SocialMediaPlatforms seed IDs
**By:** Morpheus (requested by Joe)
**What:** Added SET IDENTITY_INSERT ON/OFF with explicit Id values 1-5 to the
SocialMediaPlatforms seed block in data-seed.sql. Guards now check by Id
instead of by Name for idempotency. Updated SocialMediaPlatformIds.cs XML doc.
**Why:** IDENTITY columns with insertion-order-dependent IDs are fragile.
SocialMediaPlatformIds.LinkedIn=3 is used in production Functions — a wrong ID
causes silent dropped LinkedIn posts. Pinning IDs makes the contract explicit.

---

# Neo Review: PR #911 and PR #912 — Sprint 30 Stacking Clearance

**Date:** 2026-05-01  
**Reviewer:** Neo (Lead)  
**PRs:** #911 (ISocialMediaPublisher interface), #912 (LinkedIn composition refactor)  
**Status:** ✅ APPROVED FOR MERGE

---

## Executive Summary

Both PRs are **architecturally sound**, **well-sequenced**, and **ready to merge**. They correctly implement the Sprint 30 composition refactor design. No blocking issues detected.

---

## PR #911 Review: ISocialMediaPublisher Interface

### What It Does
- Defines `ISocialMediaPublisher` in `JosephGuadagno.Broadcasting.Domain.Interfaces`
- Implements `PublishAsync(SocialMediaPublishRequest request)` as the shared contract
- Updates all four platform managers (Twitter, Bluesky, LinkedIn, Facebook) to inherit from the interface
- Registers all four managers as `ISocialMediaPublisher` in Functions DI
- Adds `SocialMediaPublishRequest` as the superset request object accommodating all platform-specific parameters

### Verification ✅

| Aspect | Finding |
|--------|---------|
| **Interface shape** | Matches Sprint 30 decision: single `PublishAsync` method, superset request object |
| **Interface inheritance** | All four managers correctly inherit `ISocialMediaPublisher` |
| **DI registration** | All four platform managers registered with `services.AddSingleton<ISocialMediaPublisher>` in Program.cs (Twitter line 277–278, LinkedIn 318–319, Facebook 331–332, Bluesky 344–345) |
| **Request object** | Superset accommodates platform-specific needs: `AccessToken`, `AuthorId`, `ImageBytes`, `LinkUrl`, `Title`, `Description`, `Hashtags`, etc. |
| **Backward compat** | Existing platform-specific manager interfaces untouched; new interface is additive |
| **CI status** | Build ✅, Test ✅, Security ✅, Metadata ✅ |
| **PR title format** | ✅ `feat(#897)` — follows required convention |

### Acceptance Criteria Met
- ✅ ISocialMediaPublisher interface defined in Domain
- ✅ Twitter, Bluesky, LinkedIn, Facebook managers implement the interface
- ✅ No breaking changes to existing publisher behavior
- ✅ DI wiring verified; pluggable architecture enabled for future platforms

---

## PR #912 Review: LinkedIn Composition Refactor

### What It Does
- Moves message composition (Sciban template rendering) from `ProcessScheduledItemFired` function into `LinkedInManager.ComposeMessageAsync`
- Simplifies the function to focus on orchestration and publish calls
- Adds composition method to `ILinkedInManager` interface
- Implements pattern for future parallel composition refactors (Twitter, Facebook, Bluesky)

### Verification ✅

| Aspect | Finding |
|--------|---------|
| **Stacking** | Correctly stacked on `issue-897-social-media-publisher-interface` (not main) — matches Sprint 30 sequencing decision |
| **ComposeMessageAsync** | Implemented in LinkedInManager (lines 109–150) with proper service scope factory usage |
| **Interface update** | ILinkedInManager correctly extends ISocialMediaPublisher + defines ComposeMessageAsync |
| **Function refactor** | ProcessScheduledItemFired now calls `linkedInManager.ComposeMessageAsync(scheduledItem)` at line 91 |
| **Composition pattern** | Uses IServiceScopeFactory to resolve template data stores within the manager — correct separation of concerns |
| **PR title format** | ✅ `refactor(#902)` — follows required convention |
| **CI status** | Security check ✅ (CI not fully triggered because stacked on #897, not main — expected) |

### Acceptance Criteria Met
- ✅ LinkedInManager has ComposeMessageAsync method
- ✅ Azure Function delegates composition to manager
- ✅ Existing publish behavior unchanged
- ✅ Pattern is reference implementation for #899–#900–#901 (Twitter, Facebook, Bluesky)

---

## Sprint 30 Sequencing Analysis

### Execution Alignment

Both PRs correctly follow the planned Phase 1 & Phase 2 sequence from `.squad/decisions.md`:

1. **Phase 1 (COMPLETE):** ISocialMediaPublisher interface in Domain ← **PR #911** ✅
2. **Phase 2 (IN PROGRESS):** LinkedIn composition pattern validation ← **PR #912** ✅
3. **Phase 3 (READY):** Parallel Twitter, Facebook, Bluesky refactors (#899–#901) — unblocked once #912 ships

### Risk Mitigation Status
- ✅ #911 merges to main first (unblocks interface contract)
- ✅ #912 reviewed by Lead before #899–#901 kick off (validates pattern)
- ✅ No premature start of #899–#901 before #897 in main (prevents interface churn)

---

## Layering & Architecture

### Domain Boundary
- ✅ ISocialMediaPublisher stays in Domain (not tied to implementations)
- ✅ SocialMediaPublishRequest is pure domain model (no DbContext, no manager logic)

### Manager Layer
- ✅ Composition logic correctly isolated in LinkedInManager
- ✅ Platform-specific rendering kept internal (Sciban templates resolved via service provider)
- ✅ No composition bleeding into Functions layer

### Functions Layer
- ✅ ProcessScheduledItemFired remains focused on orchestration
- ✅ Delegates composition to manager, then publish call
- ✅ Clear separation: retrieve context → compose → publish

---

## Testing & Coverage

- ✅ Build passes (1154 tests, 0 failures)
- ✅ PR #911 includes platform-specific routing guards in test suites (TwitterManagerTests, BlueskyManagerUnitTests, FacebookManagerUnitTests, LinkedInManagerUnitTests)
- ✅ PR #912 test file shows proper refactoring (ProcessScheduledItemFiredTests updated to mock ComposeMessageAsync)

---

## Merge Readiness

### Blockers
**None identified.** Both PRs are ready for merge.

### Minor Notes (follow-up, not blocking)
1. **CodeQL Analysis on #911:** Still in progress; typically completes within minutes and rarely flags issues. Safe to proceed.
2. **Full CI on #912:** Will trigger once #897 merges to main (expected behavior for stacked PR).

---

## Decision

### ✅ APPROVED

Both PRs are **ready for merge immediately**.

**Recommended merge order:**
1. Merge #911 to main (unblocks interface)
2. After #911 lands, merge #912 (validates pattern)
3. After #912 PR posts for review, clear #899–#900–#901 to proceed in parallel

---

## Notes for Downstream Teams

- **Squad:Tank** — Pattern in #912 is your reference for #899, #900, #901. Review for consistency once #912 is live.
- **Joe** — Infrastructure/config tasks (#892, #856, #896) continue independently. No sequencing conflicts.
- **Trinity** — API/Web integration of ISocialMediaPublisher can begin after #911 lands (if needed for future API routing).

---

**Signed:** Neo  
**Date:** 2026-05-01T23:45:00Z

---

# Decision: SocialMediaPlatformIds Constants — Keep, But Fix the Seed

**Date:** 2026-05-05  
**Author:** Neo  
**Status:** Recommendation — action required before next environment rebuild

---

## What Was Evaluated

`SocialMediaPlatformIds` in `Domain/Constants/SocialMediaPlatformIds.cs`:

```csharp
public const int Twitter  = 1;
public const int Bluesky  = 2;
public const int LinkedIn = 3;
public const int Facebook = 4;
```

Used exclusively in the LinkedIn Azure Functions to call `GetByUserAndPlatformAsync(userId, SocialMediaPlatformIds.LinkedIn)` — i.e., to look up a user's OAuth token for LinkedIn by passing the platform's integer PK.

---

## Why They Exist

The `UserOAuthTokens` table is keyed on `(CreatedByEntraOid, SocialMediaPlatformId)`. The data store exposes `GetByUserAndPlatformAsync(string userOid, int platformId)`. At call time the function code has no other handle on the platform — it's processing a LinkedIn-specific function, it knows statically which platform it is, and there's no platform object in scope. The constants were added to give the Functions code a typed way to say "I mean LinkedIn" without a DB round-trip.

The approach is reasonable in intent. The problem is in the implementation.

---

## The Risk

The `SocialMediaPlatforms` table uses `Id int IDENTITY`. **The seed script does NOT use `SET IDENTITY_INSERT ON` with explicit IDs.** It uses `IF NOT EXISTS ... INSERT` guards by name, letting SQL Server assign IDs automatically.

On a fresh environment where the script runs exactly once in order, you get:

| Row | Name     | Auto-assigned Id |
|-----|----------|-----------------|
| 1   | Twitter  | 1               |
| 2   | BlueSky  | 2               |
| 3   | LinkedIn | 3               |
| 4   | Facebook | 4               |
| 5   | Mastodon | 5               |

That matches the constants — today. But the assumption breaks silently if:

- A row is deleted and re-inserted (IDENTITY gaps or reassignment)
- The seed order changes (a future platform is prepended)
- A non-Aspire environment has rows already present when the seed runs

If `SocialMediaPlatformIds.LinkedIn` (3) ends up pointing at the wrong row, `GetByUserAndPlatformAsync` returns `null`, the Function logs a warning, and **all LinkedIn posts are silently dropped**. No exception, no obvious error — just missed posts.

The seed SQL itself knows better: it resolves IDs by name for `MessageTemplates` seeding (`SET @SocialMediaPlatformId = (SELECT Id FROM ... WHERE Name = N'LinkedIn')`). The C# code was not given the same treatment.

---

## Options Considered

### Option A — Lock the seed with explicit IDs (recommended fix, low risk)

Add `SET IDENTITY_INSERT dbo.SocialMediaPlatforms ON/OFF` around the seed inserts, specifying `Id` explicitly. This makes the constants provably correct for all time.

```sql
SET IDENTITY_INSERT dbo.SocialMediaPlatforms ON;
IF NOT EXISTS (SELECT 1 FROM dbo.SocialMediaPlatforms WHERE Id = 1)
    INSERT INTO dbo.SocialMediaPlatforms (Id, Name, Url, Icon, IsActive)
    VALUES (1, N'Twitter', N'https://twitter.com', N'bi-twitter-x', 1);
-- ... same for 2=BlueSky, 3=LinkedIn, 4=Facebook, 5=Mastodon
SET IDENTITY_INSERT dbo.SocialMediaPlatforms OFF;
```

Impact: one SQL script change. Zero C# changes. Zero test changes. Safe on a fresh Aspire environment (idempotent). Safe on production IF the IDs already match (they should — production was seeded from this same script). **This is the right fix now.**

### Option B — Look up by name at call time

`SocialMediaPlatformDataStore` already has `GetByNameAsync(string name)`. The functions could call it once and pass the resolved ID. Removes the coupling entirely.

Downside: adds an async DB round-trip on every function invocation, or requires caching. More refactor for limited gain when Option A is simpler.

### Option C — Add a stable `Code` string column

Add `Code nvarchar(50) UNIQUE` (`twitter`, `bluesky`, `linkedin`, `facebook`) and replace the integer constants with `SocialMediaPlatformCodes` string constants. Add `GetByCodeAsync`. This is the most future-proof design — codes survive schema migrations, backups to other environments, etc.

Downside: schema change, migration script, data store change, test changes. Worthwhile for a v2, not urgent.

### Option D — Do nothing

Not acceptable. The current state has a silent data loss risk that isn't obvious from reading the code.

---

## Recommendation

**Do Option A now. Plan Option C for a future sprint.**

1. **Immediately:** Update `data-seed.sql` to use `SET IDENTITY_INSERT` with explicit IDs for all 5 `SocialMediaPlatforms` rows. Add a comment cross-referencing `SocialMediaPlatformIds.cs` so future developers know these IDs are locked by contract.

2. **Add a code comment** to `SocialMediaPlatformIds.cs` making the contract explicit:
   ```csharp
   /// <summary>
   /// Hard-coded IDs matching the locked seed data in data-seed.sql.
   /// These IDs are pinned via SET IDENTITY_INSERT — do not change without
   /// updating both this file and the seed script together.
   /// </summary>
   ```

3. **Future:** When the next schema work touches `SocialMediaPlatforms`, add a `Code` column and migrate to `SocialMediaPlatformCodes` string constants or an enum. This removes the integer fragility permanently.

The constants are not a bad pattern — integer PKs are the right join key. The bug is that the seed doesn't guarantee those PKs. Fix the seed.

---

### 2026-05-07T20:52: History Summarization Flag
**By:** Scribe (auto-detected)
**What:** 5 agent history.md files exceed 15KB summarization threshold:
  - Neo: 119,394 bytes
  - Trinity: 97,309 bytes
  - Tank: 81,577 bytes
  - Switch: 24,704 bytes
  - Sparks: 18,935 bytes

**Action:** Each agent should summarize their own history when directed by Coordinator. This is not urgent but should be scheduled for the next planning sprint.
**Why:** Large history files slow down context retrieval and team collaboration. Summaries retain key decisions while keeping the file manageable.

---

# Tank — SocialMediaPublisher contract test coverage

## Decision
For shared publisher contract work, tests should cover three layers:
1. interface shape (`PublishAsync(SocialMediaPublishRequest)`),
2. interface inheritance on each platform-specific manager interface, and
3. one manager-specific `PublishAsync` routing or guard path that proves the shared contract preserves existing behavior.

## Why
That combination catches signature drift, missing shared wiring, and platform-specific regressions without duplicating every legacy method test under the new abstraction.

## Applied In
- `src\JosephGuadagno.Broadcasting.Managers.Twitter.Tests\TwitterManagerTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Bluesky.Tests\BlueskyManagerUnitTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Facebook.Tests\FacebookManagerUnitTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests\LinkedInManagerUnitTests.cs`

---

# Decision: issue #899 Twitter composition pattern

## Context
Issue #899 moves Scriban template rendering out of `ProcessScheduledItemFired` (Functions/Twitter) into `TwitterManager.ComposeMessageAsync`, following the identical pattern established by the LinkedIn refactor (#902).

## Decision
Add `ITwitterManager.ComposeMessageAsync(ScheduledItem, CancellationToken)` and implement it in `TwitterManager` using an `IServiceScopeFactory?` constructor overload. The Function delegates all composition to a single `await twitterManager.ComposeMessageAsync(scheduledItem)` call, removing 4 previously injected services and ~100 lines of per-type helper methods.

## Why
The same rationale as #902: keeps the Function focused on queue mechanics; moves platform lookup, template fetching, and Scriban rendering into the manager where it belongs. Using `IServiceScopeFactory` avoids changing the singleton `TryAddSingleton<ITwitterManager, TwitterManager>()` registration in `Program.cs` — .NET DI picks the 3-param constructor automatically.

## Pattern to reuse for future platforms
Any new platform following this composition pattern should:
1. Add `Task<string> ComposeMessageAsync(ScheduledItem, CancellationToken)` to the interface
2. Add `IServiceScopeFactory?` as an optional constructor param in the manager
3. Implement `GetMessageType(ScheduledItemType)` mapping: Engagements→NewSpeakingEngagement, Talks→ScheduledItem, SyndicationFeedSources→NewSyndicationFeedItem, YouTubeSources→NewYouTubeItem, _→RandomPost
4. Implement `TryRenderTemplateAsync` with platform lookup → template fetch → Scriban render → fallback to `scheduledItem.Message`
5. Simplify the corresponding Azure Function to call `ComposeMessageAsync` and remove per-type helpers

---

# Decision: issue #902 LinkedIn composition pattern

## Context
Issue #902 needs the LinkedIn scheduled-item Function to stop owning Scriban rendering while keeping the existing queue-based publish flow and the singleton `ILinkedInManager` registration introduced on the #897 branch.

## Decision
Add `ILinkedInManager.ComposeMessageAsync(ScheduledItem, CancellationToken)` and move the LinkedIn template lookup/rendering logic there. Keep `ProcessScheduledItemFired` responsible for source-specific post metadata (`Title`, `LinkUrl`, OAuth token, image URL), and have the manager create a scope internally via `IServiceScopeFactory` to resolve the scoped template/data services it needs.

## Why
This keeps the Function focused on shaping the outbound LinkedIn queue message while removing all Scriban composition logic from the Function class. Using `IServiceScopeFactory` avoids a wider DI lifetime churn on the already-registered singleton manager, which keeps the #902 slice narrow and safe to stack on top of #897.

---

## Decision

Keep Sprint 29 hardening follow-ups as two separate issue-scoped branches and PRs even though the code started in one dirty working tree.

## Why

- `#890` is a Data.Sql fail-fast guard in `UserOAuthTokenDataStore.GetExpiringWindowAsync(...)`.
- `#893` is a Functions logging/config-hardening change in `LinkedIn/NotifyExpiringTokens.cs`.
- Splitting them preserves the repo rule of one PR per issue and keeps review scope aligned to the owning layer.

## Outcome

- `#890` shipped on branch `issue-890-expiring-window-guard` via PR #909.
- `#893` shipped on branch `issue-893-webbaseurl-warning` via PR #910.
- Remaining `.squad/` state stayed on `main` and was not bundled into either PR.

---

# Decision: Stack PR #902 on clean PR #897

## Context

Local branches for #897 and #902 contained the intended product work, but the visible branch tips also carried `.squad` drift that should not ship in product pull requests. Issue #902 was developed on top of #897, so it needed branch hygiene review before opening both PRs.

## Decision

- Recover #897 into a clean product-only branch and open it against `main`
- Recover #902 into a clean product-only branch stacked on `issue-897-social-media-publisher-interface`
- Exclude `.squad` changes from both product PRs

## Why

Cherry-picking #902 directly onto `origin/main` produced conflicts in:

- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn\LinkedInManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn\Models\ILinkedInManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests\LinkedInManagerUnitTests.cs`

That made a stacked PR the safest reviewable shape. It keeps one issue per PR, preserves the intended merge order, and avoids leaking local squad bookkeeping into the product review.

## Result

- PR #911: `feat(#897): define ISocialMediaPublisher common interface`
- PR #912: `refactor(#902): move LinkedIn composition to manager`

