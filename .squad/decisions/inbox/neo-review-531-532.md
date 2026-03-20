# Decision: PR Review — #531 and #532 Merge Strategy

**Date:** 2026-03-20  
**Decision Maker:** Neo (Lead)  
**Context:** Post-review of PRs #531 (Trinity: regression test for Talks.View scope) and #532 (Ghost: incremental consent via AuthorizeForScopes)

## Decision

Both PRs #531 and #532 were **MERGED** (already merged before my explicit approval — likely by Joseph or another agent monitoring CI).

### PR #531 — Talks.View Scope Regression Test
- **Status:** Squash-merged, branch deleted, issue #527 closed
- **Verdict:** CLEAN — test-only PR, CI green, adds valuable regression coverage
- **Key takeaway:** When a bug is fixed in one PR (#526), a follow-up test-only PR is appropriate and low-risk

### PR #532 — Incremental Consent for MVC Controllers
- **Status:** Squash-merged, branch deleted, issue #528 closed
- **Verdict:** CLEAN — correct use of `[AuthorizeForScopes]` attribute at class level
- **Key takeaway:** `[AuthorizeForScopes]` without parameters is correct when scopes are globally configured in `EnableTokenAcquisitionToCallDownstreamApi()` (Program.cs line 72-78)

## Rationale

1. **PR #531 (regression test):**
   - Trinity added `GetTalkAsync_WithViewScope_ReturnsTalk` to verify fine-grained scope acceptance
   - Issue #527 was already fixed by Ghost in PR #526; this test prevents future regressions
   - All 42 API tests pass, GitGuardian clean
   - No code changes, only test addition — minimal risk

2. **PR #532 (incremental consent):**
   - Ghost applied `[AuthorizeForScopes]` at class level on 4 API-calling controllers (Engagements, Talks, Schedules, MessageTemplates)
   - Attribute catches `MsalUiRequiredException` (wrapped as `MicrosoftIdentityWebChallengeUserException`) and triggers incremental consent flow instead of 500 error
   - SQL-backed token cache confirmed in place (Program.cs lines 89-94)
   - No ScopeKeySection parameter needed — scopes auto-discovered from global registration
   - CI green, no code logic changes

## Implications for Team

- **Test-only PRs are valid:** When a bug is fixed, a follow-up regression test PR is encouraged (see #531)
- **AuthorizeForScopes pattern:** Class-level `[AuthorizeForScopes]` is correct for controllers that consistently call downstream APIs; no parameter needed when scopes are globally registered
- **Incremental consent is now operational:** Web app will handle evicted MSAL tokens gracefully by re-prompting users instead of throwing 500 errors

## Action Items

- None — both PRs are merged and functioning as expected
- Sprint 8 issues #527 and #528 are now closed
