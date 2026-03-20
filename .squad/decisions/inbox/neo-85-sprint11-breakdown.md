# Decision: MSAL Exception Handling Sprint 11 Breakdown

**Date:** 2026-03-21  
**Decider:** Neo (Lead)  
**Status:** Approved  
**Issue:** #85 - Handle Exceptions with Microsoft Entra (Microsoft Identity) login

## Context

Ghost posted a comprehensive 3-phase MSAL exception handling plan in Issue #85 to address graceful error handling for Azure AD/Entra authentication failures in the Web application. The plan covers:

1. **Phase 1 (Critical Login Path):** OpenID Connect event handlers + dedicated auth error page
2. **Phase 2 (Global Safety Net):** Global MSAL exception middleware + hardened error page
3. **Phase 3 (Token Cache Resilience):** Cache collision detection/recovery (addresses #83)

Current state:
- ✅ [AuthorizeForScopes] on API-calling controllers (PR #532) handles MsalUiRequiredException
- ✅ RejectSessionCookieWhenAccountNotInCacheEvents handles user_null errors
- ❌ No handling for initial login failures (OpenIdConnectProtocolException)
- ❌ No global MSAL exception middleware
- ❌ Generic error page exposes technical details

## Decision

Break the work into **5 independently mergeable sub-issues** for Sprint 11, ordered by dependency:

### Phase 1: Critical Login Path

**#544 - feat(web): Add OpenID Connect event handlers for login failures**
- **Scope:** Implement OnRemoteFailure and OnAuthenticationFailed handlers in Program.cs
- **Files:** `Program.cs` (add Configure<OpenIdConnectOptions> block)
- **Dependencies:** None
- **Why separate:** Minimal single-file change; handles initial login failures (highest user impact)

**#545 - feat(web): Add dedicated AuthError page and view model**
- **Scope:** Create AuthErrorViewModel, HomeController.AuthError action, AuthError.cshtml view
- **Files:** New AuthErrorViewModel.cs, AuthError.cshtml, update HomeController.cs
- **Dependencies:** None (can be done in parallel with #544)
- **Why separate:** Pure UI addition; no risk to existing auth flow

### Phase 2: Global Safety Net

**#546 - feat(web): Add global MsalExceptionMiddleware**
- **Scope:** Middleware to catch MSAL exceptions outside OIDC flow
- **Files:** New Middleware/MsalExceptionMiddleware.cs, update Program.cs
- **Dependencies:** Depends on #545 (needs AuthError page for redirects)
- **Why separate:** Adds new middleware layer; requires correct pipeline ordering

**#547 - fix(web): Harden Error.cshtml to hide Request ID in production**
- **Scope:** Environment-aware error rendering (hide technical details in production)
- **Files:** `Views/Shared/Error.cshtml`
- **Dependencies:** None (can be done in parallel with #546)
- **Why separate:** Isolated single-file security hardening; independent value

### Phase 3: Token Cache Resilience

**#548 - feat(web): Add token cache collision resilience to cookie validation**
- **Scope:** Enhance RejectSessionCookieWhenAccountNotInCacheEvents to handle multiple_matching_tokens_detected
- **Files:** `Infrastructure/RejectSessionCookieWhenAccountNotInCacheEvents.cs`
- **Dependencies:** Depends on #546 (middleware as fallback if cache clearing fails)
- **Why separate:** Addresses distinct Issue #83; requires cache clearing extensions

## Rationale

**Why this breakdown:**
1. **Independent merge-ability:** Each issue = one PR, no code conflicts
2. **Testability:** Each change testable in isolation (manual auth scenarios)
3. **Risk management:** Phase 1 (high user impact) lands first; Phase 3 (edge case) lands last
4. **Parallelization:** #544/#545 parallel, #546/#547 parallel = faster completion
5. **Ghost ownership:** All labeled `squad:ghost` (auth/security expert)

**Why NOT combine:**
- ❌ Combining #544 + #545 = one large PR mixing logic (Program.cs) and UI (views) = harder review
- ❌ Combining #546 + #548 = middleware + cache logic in one PR = testing complexity
- ✅ Current split: reviewers can approve #544 (logic) and #545 (UI) independently

## Recommended Merge Order

```
#544 (OIDC handlers) → #545 (AuthError page) → #546 (Middleware) → #547 (Error.cshtml) → #548 (Cache resilience)
          ↓                      ↓                       ↓                   ↓                      ↓
       Phase 1a                Phase 1b              Phase 2             Phase 2              Phase 3
```

**Critical path:** #545 must land before #546 (middleware redirects to AuthError page)  
**Parallel opportunities:** #544 ∥ #545 (both Phase 1), #546 ∥ #547 (both Phase 2)

## Consequences

**Positive:**
- Sprint 11 has clear, actionable work items for Ghost
- Each PR small enough for same-day review/merge
- Failure in one phase doesn't block subsequent phases (e.g., if #548 cache clearing blocked by library limitations, #544-#547 still deliver user-facing improvements)

**Negative:**
- 5 separate PRs = 5 CI runs, 5 reviews, 5 merges (vs. 1-2 larger PRs)
- Dependency chain (#545 → #546 → #548) means some waiting

**Mitigation:**
- Label all issues `sprint:11` for visibility
- Document dependencies in issue bodies ("Depends on #X")
- Ghost can work on #544 + #545 in parallel to front-load work

## Labels Applied

All issues: `enhancement,web-ui,squad:ghost,sprint:11`

## References

- **Parent Issue:** #85 - Handle Exceptions with Microsoft Entra (Microsoft Identity) login
- **Related Issue:** #83 - Token cache collision (multiple_matching_tokens_detected)
- **Related PR:** #532 - Added [AuthorizeForScopes] to API-calling controllers
- **Ghost's Plan:** https://github.com/jguadagno/jjgnet-broadcast/issues/85#issuecomment-4101036534
