# Neo — History

## Summary

Lead reviewer and sprint planner. Primary domain: architecture, CI/CD, patterns, code reviews, issue triage.

**Current focus:** Sprint 12 planning. Sprint 11 complete — all 5 PRs (#551–#555) merged to main, all 5 issues (#544–#548) closed. Three-layer auth exception defence for issue #85 fully delivered. Sprint 12 tagged with 13 issues.

**Key patterns established:**
- DTO/API: request DTOs exclude route params, return Task<ActionResult<T>>, null guard before ToResponse
- Pagination: guard divide-by-zero, clamp page≥1, pageSize 1–100
- Database: SaveChangesAsync override covers both variants
- Testing: sealed types use typed null, never Mock.Of<SealedType>()
- PR review: always verify diff against body, check issue status before review

**Backlog triage complete:** 32 issues assigned to 6 squads (neo:12, sparks:7, switch:8, trinity:2, morpheus:2, ghost:1).

**Sprint closure:** Sprint 9 (7 issues) complete via PRs #516–#526, all merged. Sprint 11 (5 issues) complete via PRs #551–#555, all merged. Three-layer auth exception defence live on main.

## Recent Work

### 2026-03-21: Sprint 11 Closeout — All PRs Merged

All 5 sprint 11 PRs merged. All 5 issues closed.

| Layer | PR | Issue | Description |
|-------|----|-------|-------------|
| Layer 1 | #555 | #548 | `RejectSessionCookieWhenAccountNotInCacheEvents` catches `multiple_matching_tokens_detected` |
| Layer 2 | #554 | #546 | `MsalExceptionMiddleware` — global fallback (after UseRouting, before UseAuthentication) |
| Layer 3 | #553 | #544 | `Program.cs` OIDC handlers — `OnRemoteFailure` AADSTS code mapping + `OnAuthenticationFailed` |
| Support | #551 | #545 | `AuthError` page + `AuthErrorViewModel` + `[AllowAnonymous]` |
| Support | #552 | #547 | `Error.cshtml` hardened with `IsDevelopment()` gate |

Sprint 12 tagged with 13 issues.

---

### 2026-03-21: Sprint 11 Ghost PR Review — Issues #544–#547

**PRs reviewed:** #551, #552, #553, #554 (all part of issue #85 — Handle Exceptions with Microsoft Entra login)

**Verdicts:**

| PR | Title | Verdict | Reason |
|----|-------|---------|--------|
| #551 | AuthError page & ViewModel | ✅ APPROVED | [AllowAnonymous] present, correct ViewModel, Razor auto-encoding for XSS protection, ResponseCache correct |
| #552 | Error.cshtml hardening | ✅ APPROVED | IsDevelopment() used correctly, 8-char reference safe, Dev advisory correctly gated |
| #553 | OIDC event handlers | ❌ CHANGES REQUESTED | Program.cs changes completely missing — PR body describes OnRemoteFailure/OnAuthenticationFailed handlers but they are not in the diff |
| #554 | MsalExceptionMiddleware | ✅ APPROVED | All 4 MSAL exception types handled, middleware ordering correct (after UseRouting, before UseAuthentication), log levels appropriate, CI passes |

**Critical finding (PR #553):** The issue-544 branch contains only duplicates of PR #551 and PR #552 files. The actual OIDC event handler code in Program.cs (OnRemoteFailure, OnAuthenticationFailed, error code mapping) was never committed.

**Merge sequence recommendation:** #551 → #552 → #553 → #554 (sequential merge avoids Error.cshtml conflict).

---

### 2026-07-14: PR #555 Review — Token Cache Collision Resilience (Issue #548)

**PR:** #555 — `feat(web): Add token cache collision resilience to cookie validation`
**Author:** Ghost | **Closes:** #548 | **Part of:** #83/#85

**Verdict: ✅ APPROVED** (posted as comment — GitHub blocked self-approval on owner account)

**Checklist results:**

| Check | Result |
|---|---|
| `user_null` path preserved | ✅ Untouched |
| Catch block ordering | ✅ Orthogonal types; no shadowing |
| Log level | ✅ LogWarning (correct for recoverable condition) |
| `context.RejectPrincipal()` | ✅ Called correctly |
| Two-layer defence | ✅ Sound (ValidatePrincipal + MsalExceptionMiddleware) |
| Build | ✅ 0 errors |

**Notes:** `ILogger` resolved via `GetService<T>()` at request time is correct for singleton events class. `Error.cshtml` Dev-only RequestId gating is a good security bonus included in this PR.

---

### 2026-07-14: PR #553 Re-review — OIDC Event Handlers (Issue #544)

**PR:** #553 — `feat(web): Add OpenID Connect event handlers for login failures`  
**Author:** Trinity (branch correction after original Changes Requested)

**Verdict: ✅ APPROVED** (posted as comment — GitHub blocked self-approval on owner account)

**Checklist results:**

| Check | Result |
|---|---|
| `Program.cs` with `Configure<OpenIdConnectOptions>` block | ✅ Present |
| `OnRemoteFailure` maps AADSTS650052/700016/invalid_client to sanitized messages | ✅ |
| `OnAuthenticationFailed` handles generic failures | ✅ |
| `context.HandleResponse()` called before redirects in both handlers | ✅ |
| Full exception logged via `ILogger<Program>` (runtime `GetRequiredService`) | ✅ |
| No raw Azure AD error messages exposed to user | ✅ |
| Build: 0 errors (per Joseph's confirmation) | ✅ |

**Minor observation:** `Error.cshtml` in diff appears to be a rebase artifact carrying the #547 hardening change forward — idempotent, not harmful. Flagged in review comment.

**Root cause of original failure:** Ghost committed duplicates of #545/#547 files into issue-544 branch without the actual Program.cs OIDC handler code. Trinity + Scribe corrected the branch.

---

*For earlier work, see git log and orchestration-log/ records.*

---

### 2026-07-14: PR #557 Review — Production Approval Gate + Stop Staging Slot (Issue #556)

**PR:** #557 — `ci: add production approval gate and stop staging slot after swap`
**Verdict: ✅ APPROVED** (posted as comment — GitHub blocks self-approval on owner account)

**What the PR does:**
1. Adds `environment: production` gate to `swap-to-production` job in all three workflows
2. Adds `az webapp stop --slot staging` / `az functionapp stop --slot staging` after each swap

**Checklist:**

| Check | API | Web | Functions |
|---|---|---|---|
| `environment: production` gate | ✅ | ✅ | ✅ |
| Correct CLI command | ✅ `az webapp stop` | ✅ `az webapp stop` | ✅ `az functionapp stop` |
| `--slot staging` flag | ✅ | ✅ | ✅ |
| Correct app name | ✅ | ✅ | ✅ |
| Stop skipped if swap fails | ✅ default | ✅ default | ✅ default |
| YAML valid | ✅ | ✅ | ✅ |

**Non-blocking observations:**
1. API and Web place "Stop staging slot" AFTER "Get production URL" — if the URL-fetch fails post-swap, staging slot stays running. Correct fix: move stop step before get-URL step. Functions workflow already has correct ordering.
2. `if: success()` not explicit on stop step — intent is implicit via default GHA behaviour. Acceptable but documenting with explicit condition is cleaner.

## Learnings

### 2026-07-15: Issue Specs Batch — #591 #575 #574 #573

Full specs written to `.squad/sessions/issue-specs-591-575-574-573.md`.

**Key paths discovered:**
- `src/JosephGuadagno.Broadcasting.Api/Program.cs` — `ConfigureTelemetryAndLogging()` has no Serilog MinimumLevel (defaults Verbose) — root of #591
- `src/JosephGuadagno.Broadcasting.Functions/Program.cs` — Has `#if DEBUG/.MinimumLevel.Debug()/.MinimumLevel.Warning()` guard — correct direction but Warning is too strict
- `src/JosephGuadagno.Broadcasting.Domain/Models/PagedResponse.cs` — existing paged API response type; NOT suitable as data-store return type
- `src/JosephGuadagno.Broadcasting.Data.Sql/MappingProfiles/BroadcastingProfile.cs` — SQL↔Domain; no Api DTO mappings
- `src/JosephGuadagno.Broadcasting.Web/MappingProfiles/WebMappingProfile.cs` — Domain↔ViewModel; no Api DTO mappings
- No `ApiBroadcastingProfile` exists yet — needed for #575

**Patterns established:**
- New `PagedResult<T>` type in Domain.Models for data-store paged returns (Items + TotalCount only)
- Route-derived fields (Id, EngagementId, Platform, MessageType) are Ignored in AutoMapper Request→Model maps; set manually in controller post-map
- Web paging: services return `PagedResponse<T>`, controllers populate ViewBag, shared `_PaginationPartial.cshtml` reads ViewBag
- Logging: `MinimumLevel.Information()` + `Override("Microsoft", Warning)` + `Override("System", Warning)` is the target prod config for both Api and Functions

**Team assignments:**
- #591 → Link | #575 → Trinity | #574 data stores → Morpheus | #574 managers+controllers → Trinity | #573 services+controllers → Switch | #573 views → Sparks | Tests → Tank

**Dependency order:** #591 first (standalone), then #574 Morpheus, then #575 + #574 Trinity in parallel, then #573.

### 2026-07-14: MSAL Auth Broken — Revert PRs #500 #553 #554 #555

**Trigger:** Joseph reported that the merged Sprint 11 auth PRs (plus PR #500 security headers) broke MSAL authentication.

**PRs reverted (newest-first, single combined commit):**

| PR | Commit | Description |
|----|--------|-------------|
| #553 | `6d25597` | OIDC event handlers — squash merge |
| #555 | `34b597b` | Token cache collision resilience — squash merge |
| #554 | `b8e5169` | MsalExceptionMiddleware — squash merge |
| #500 | `663a76d` | Security headers middleware — true merge commit (`-m 1`) |

**Branch:** `revert/msal-prs-500-553-554-555`
**PR:** [#572](https://github.com/jguadagno/jjgnet-broadcast/pull/572)

**Key execution notes:**
- PRs #553–#555 were squash merges (single commit on main) — reverted directly
- PR #500 was a true merge commit — required `git revert -m 1 <sha>`
- Squad doc files (neo/trinity/ghost history.md, decisions.md) conflicted during revert of #553; resolved by keeping HEAD (post-merge doc updates are not part of the MSAL issue)
- All 9 code files staged into a single revert commit



### CI/CD: Step ordering for cleanup steps

Place cleanup/stop steps immediately after the primary action step, not after informational steps. If the info step fails, downstream steps won't run and cleanup is skipped.

```yaml
# ✅ Correct ordering
- name: Swap
- name: Stop staging slot   # cleanup immediately after primary action
- name: Get production URL  # info last — its failure doesn't skip cleanup

# ⚠️ Fragile — URL-fetch failure skips stop
- name: Swap
- name: Get production URL
- name: Stop staging slot
```

### Integration test projects: InMemoryCredentialStore config binding

LinqToTwitter's `InMemoryCredentialStore` uses property names `OAuthToken` / `OAuthTokenSecret` (NOT `AccessToken`/`AccessTokenSecret`). Config key `Twitter:OAuthToken` binds correctly. Confirmed by existing Functions `local.settings.json` and `Program.cs`.

### Azure CLI: slot stop commands

- Web Apps: `az webapp stop --name <name> --resource-group <rg> --slot <slot>`
- Function Apps: `az functionapp stop --name <name> --resource-group <rg> --slot <slot>`

Both commands are symmetric and both accept `--slot`.

### GitHub Actions: `environment:` scalar vs object

Both forms are valid YAML for GHA environments:
- Scalar: `environment: production` (no URL — correct for Functions)
- Object: `environment:\n  name: 'production'\n  url: ${{ ... }}` (with deployment URL — correct for web apps)

### 2026-03-22: Issue #556 / PR #557 Review — Deployment Approval Gate

**Related:** Issue #556 (created by Scribe), PR #557 (Cypher, awaiting merge)

**Summary:** All three deployment workflows (API, Web, Functions) now include approval gate + staging slot cleanup.

**Review Verdict:** ✅ APPROVED (with non-blocking observations)

**Observations:**

1. **Step ordering in API + Web workflows:** "Stop staging slot" is placed after "Get production URL" step. If the URL-fetch step fails (transient Azure issue), the stop step is skipped — leaving staging running. Correct order: `Swap → Stop → Get URL`. Functions workflow already has correct order.

2. **Implicit success condition:** The cleanup step relies on all prior steps succeeding. This is correct for `stop` (we only want to stop after a successful swap), but the observation is just a note on the pattern.

**Decision Recorded:** Pattern documented in decisions.md — cleanup/stop steps should run immediately after primary action, before informational steps, to guarantee cleanup runs if primary action succeeds.

**Action:** Recommend applying correct step order in future workflow modifications or when PR #557 is revisited. Not a blocker for merge.

---

### 2026-07-14: PR #559 Review — Twitter Manager Integration Tests (Issue #558)

**PR:** #559 — `test: add Twitter Manager integration test project`
**Verdict: ✅ APPROVED** (posted as comment — GitHub blocks self-approval on owner account)

**All scope items confirmed:**

| Item | Result |
|------|--------|
| 4 test cases | ✅ |
| `[Trait("Category", "Integration")]` on class | ✅ |
| `[Fact(Skip = "Manually run only")]` on all 4 | ✅ |
| Startup.cs DI: InMemoryCredentialStore → SingleUserAuthorizer → TwitterContext → ITwitterManager | ✅ |
| Config keys: ConsumerKey/ConsumerSecret/OAuthToken/OAuthTokenSecret | ✅ |
| appsettings.Development.json with 4 blank placeholders | ✅ |
| TwitterSendTweetTests.cs deleted from Functions.IntegrationTests | ✅ |
| Project added to solution | ✅ |
| TwitterPostException for error-path assertions | ✅ |
| Tweet cleanup (delete) in success-path tests | ✅ |

**Non-blocking observations:**
1. `ProductVersion` typo in .csproj: `($VersionSuffix)` should be `$(VersionSuffix)`. No build/runtime impact (IsPackable=false, test project).
2. CancellationToken not propagated to async calls. Deleted file used `cancelToken: TestContext.Current.CancellationToken`; new tests omit it. Fine for manual-only tests.

**Outcome:**
- ✅ PR approved via review comment (2026-03-22T19:48:03Z)
- ✅ Joseph Guadagno merged PR #559 to main
- ✅ Issue #558 closed

**Pattern Documented:**
Integration test projects for social managers should:
- Use InMemoryCredentialStore for credentials (no real API calls)
- Configure DI in Startup.cs (test-project pattern)
- Mark tests with `[Trait("Category", "Integration")]` + `[Fact(Skip = "Manually run only")]`
- Implement cleanup logic in success-path tests
- Use exception assertions for error paths

**Next Steps:** Establish similar integration test projects for Facebook, LinkedIn, and Bluesky managers.

