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
