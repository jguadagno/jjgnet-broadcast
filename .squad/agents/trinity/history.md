# Trinity — History

## Summary

Backend dev. Primary domain: API layer, pagination, DTOs, message templates, scope audits.

**Current focus:** Pagination implementation (#316) and scope audit regression tests (#527).

**Key learnings:**
- Always use feature branch + PR workflow; never commit directly to main
- Check if concurrent PRs already fixed issue before implementing
- Scriban templates are database-backed via MessageTemplates table
- Sealed 3rd-party types require typed null in tests, not Mock.Of<T>()

**Implementation summary:**
- Pagination: 8 list endpoints updated with page/pageSize params (defaults: 1, 25)
- Message templates: 20 templates seeded (5 per platform) matching existing fallback logic
- Scope audit: All 34 endpoints verified for fine-grained scope support (Talks.View/All dual pattern)

## Recent Work

### 2026-03-21: Sprint 11 Closeout — All PRs Merged

Sprint 11 complete. All 5 PRs (#551–#555) merged to main. All 5 issues (#544–#548) closed. Three-layer auth exception defence for issue #85 is live on main:
- Layer 1: `RejectSessionCookieWhenAccountNotInCacheEvents` handles `multiple_matching_tokens_detected` (PR #555)
- Layer 2: `MsalExceptionMiddleware` catches MSAL exceptions globally (PR #554)
- Layer 3: `Program.cs` OIDC event handlers map AADSTS codes to friendly messages (PR #553)
- AuthError page (`[AllowAnonymous]`, ResponseCache(NoStore)) serves as the landing page (PR #551)
- Error.cshtml gated by `IsDevelopment()` — 8-char reference ID in production (PR #552)

Sprint 12 tagged with 13 issues.

---

### 2026-03-21: Fix PR #553 — Correct Branch with OIDC Event Handlers (Trinity)

- **Task:** Branch `issue-544` had wrong files committed (AuthError page, HomeController, Error.cshtml from other PRs). Program.cs changes were missing.
- **Root cause:** Ghost committed duplicate work from issues #545 and #547 into issue-544.
- **What the Scribe already did:** Reverted HomeController.cs, deleted AuthErrorViewModel.cs and AuthError.cshtml, restored Error.cshtml in local commits.
- **What I implemented:** Added `Configure<OpenIdConnectOptions>` block to `Program.cs` wiring `OnRemoteFailure` (maps AADSTS650052/700016/invalid_client to friendly messages) and `OnAuthenticationFailed` (generic error redirect). Both handlers call `context.HandleResponse()` before redirecting to `/Home/AuthError`.
- **Build:** ✅ 0 errors. Pushed to origin, commented on PR #553.
- **Lesson:** Scribe may already have partially cleaned up a branch before I work it — check local HEAD vs main carefully before re-reverting.

### 2026-03-21: Scope Audit & Regression Test for Issue #527 (Trinity)

- **Task:** Verify and add regression test for GetTalkAsync fine-grained scope support
- **Finding:** Scope was already fixed in PR #526; issue filed based on pre-merge state
- **What I Implemented:**
  - Regression test GetTalkAsync_WithViewScope_ReturnsTalk added to ensure Talks.View is accepted
  - Full audit of all 34 endpoints across 3 controllers (Engagements, Schedules, MessageTemplates)
  - No gaps found; fine-grained scope rollout from PR #526 is complete
- **PR #531 opened** with full audit table (22 Engagements endpoints, 9 Schedules, 3 MessageTemplates)
- **Lesson:** Check whether concurrent PRs already fixed the issue before adding new code

### 2026-03-21: Sprint 11 Branch Cleanup (Trinity)

- **Task:** Delete all 5 sprint 11 local branches after their PRs were squash-merged to main.
- **Branches deleted:** `issue-544` (-D), `issue-545` (-d), `issue-546` (-D), `issue-547` (-D), `issue-548` (-d)
- **Note:** `issue-545` and `issue-548` deleted cleanly with `-d`. The other three required `-D` because squash merges leave branch tips unrecognized by `git branch --merged`; confirm via `git log --oneline` on main before force-deleting.
- **Remote tracking refs:** Pruned via `git remote prune origin`; no issue-54x refs remained after.
- **Complication:** Local main had a diverged commit, requiring a merge commit during `git pull`. Also had to stash uncommitted changes on a feature branch before switching to main.

---

*For earlier work, see git log and orchestration-log/ records.*
