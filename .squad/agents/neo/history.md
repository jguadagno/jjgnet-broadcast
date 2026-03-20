# Neo — History

## Summary

Lead reviewer and sprint planner. Primary domain: architecture, CI/CD, patterns, code reviews, issue triage.

**Current focus:** Sprint 11 MSAL token cache collision handling (issues #83/#85). Ghost working on Layer 1 (ValidatePrincipal catch), Neo reviewing Layers 1–2 PRs #551–#554. Established two-layer defence pattern: cookie validation + middleware fallback.

**Key patterns established:**
- DTO/API: request DTOs exclude route params, return Task<ActionResult<T>>, null guard before ToResponse
- Pagination: guard divide-by-zero, clamp page≥1, pageSize 1–100
- Database: SaveChangesAsync override covers both variants
- Testing: sealed types use typed null, never Mock.Of<SealedType>()
- PR review: always verify diff against body, check issue status before review

**Backlog triage complete:** 32 issues assigned to 6 squads (neo:12, sparks:7, switch:8, trinity:2, morpheus:2, ghost:1).

**Sprint closure:** Sprint 9 (7 issues) complete via PRs #516–#526, all merged.

## Recent Work

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

*For earlier work, see git log and orchestration-log/ records.*
