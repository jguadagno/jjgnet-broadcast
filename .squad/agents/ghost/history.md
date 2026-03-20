# Ghost — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-03-19 | Sprint 8 #336 — Explicit cookie security options (HttpOnly, Secure, SameSite) in Web/Program.cs | ✅ PR #510 opened, merged to Sprint 8 milestone |
| 2026-03-20 | Issue #170 — Fine-grained permission scopes on all API endpoints | ✅ PR #526 opened targeting main |

## Learnings

### Issue #170 — Fine-grained API scopes

**Codebase state at time of work:**
The `Domain/Scopes.cs` file already had fine-grained scope constants defined (`List`, `View`, `Modify`, `Add`, `Delete`) for Engagements, Talks, and Schedules. All controllers had the granular `VerifyUserHasAnyAcceptedScope(...)` calls commented out and were only using `*.All`. The Web services mirrored this pattern. `MessageTemplates` only had `All` — no granular scopes defined.

**Pattern chosen: dual-scope acceptance for backward compat**
`HttpContext.VerifyUserHasAnyAcceptedScope(specificScope, *.All)` — accepts either. This means existing Azure AD tokens with `*.All` continue working without any client reconfiguration. New tokens can be issued with least-privilege scopes only.

**Web services use single specific scope per call**
The `SetRequestHeader(scope)` method on `ServiceBase` acquires a token with exactly the requested scope. Changed all Web service calls to request the fine-grained scope rather than `*.All`. This means the Web app's MSAL tokens will only carry the specific scope needed per operation, which is the correct least-privilege behavior at the token level.

**Bug discovered and fixed in EngagementService.DeleteEngagementTalkAsync**
Was requesting `Engagements.All` scope and the comment incorrectly said `Engagements.Delete`. The correct scope for deleting a talk is `Talks.Delete`. Fixed as part of this work.

**Scopes.ToDictionary vs AllAccessToDictionary**
- `ToDictionary(scopeUrl)` — all fine-grained scopes. Used for Swagger OAuth scope list (updated XmlDocumentTransformer to use this).
- `AllAccessToDictionary(scopeUrl)` — just `*.All` scopes. Still used by Web/Program.cs for MSAL `EnableTokenAcquisitionToCallDownstreamApi` (the set of scopes the web app's MSAL client is allowed to request). This was left as-is; MSAL will automatically scope-down per-request based on what each service call requests.

**Tests: *.All remains valid**
Existing unit tests pass `*.All` tokens and they continue to pass because the API accepts `specificScope OR All`. No test changes needed. The 42/42 pass rate also resolved 3 pre-existing test failures (those failures were unrelated to scope logic — they appear to have been caused by in-flight branch state on the workspace).
