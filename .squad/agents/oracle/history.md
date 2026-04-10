# Oracle — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Security Engineer
- **Joined:** 2026-03-14T16:55:20.779Z

## Learnings

<!-- Append learnings below -->

### 2025-01-XX: Pre-Feature Security Audit

**Context:** Conducted comprehensive security audit of jjgnet-broadcast codebase before new feature development.

**Findings:**
1. **Secrets Management:**
   - All appsettings.json files properly use placeholders for secrets ("Set in User Secrets")
   - Azure Key Vault integration correctly configured for Web project
   - Exception: Functions/local.settings.json contains hardcoded Application Insights instrumentation key (c2f97275-e157-434a-981b-051a4e897744) — VULNERABILITY
   - All social platform tokens (Facebook, LinkedIn, Twitter, Bluesky, YouTube) are empty strings in config — correct pattern

2. **Authentication Middleware Ordering:**
   - Api: UseAuthentication() → UseAuthorization() → UseRateLimiter() — CORRECT per team decision
   - Web: UseAuthentication() → UseUserApprovalGate() → UseAuthorization() — CORRECT (custom RBAC middleware between auth/authz)
   - HTTPS redirection enabled on both projects

3. **Authorization Coverage:**
   - All 4 API controllers have [Authorize] attribute
   - All 31 API endpoints use HttpContext.VerifyUserHasAnyAcceptedScope() for OAuth2 scope validation
   - Web has global [Authorize] filter with role-based policies (Administrator, Contributor, Viewer)
   - No PII endpoints found without authorization

4. **Session Cookie Handling:**
   - RejectSessionCookieWhenAccountNotInCacheEvents.cs correctly uses context.RejectPrincipal() ONLY (no SignOutAsync)
   - Guard against null principal prevents infinite loop (lines 16-19)
   - Handles token cache collisions (MsalServiceException, MsalClientException)
   - Pattern matches team decision — no violations found

5. **Social Platform Tokens:**
   - Token refresh logic (Facebook/RefreshTokens.cs) saves new tokens to Key Vault, not config files
   - Logs only token expiry timestamps, not token values
   - Health checks validate config presence without logging sensitive values

6. **Input Validation:**
   - No raw SQL found (no FromSqlRaw/ExecuteSqlRaw) — EF Core LINQ only (safe)
   - No XSS vulnerabilities (no @Html.Raw in views, all @Model output is Razor-encoded)
   - Global antiforgery token validation on Web POST/PUT/DELETE
   - API correctly disables antiforgery (uses Bearer tokens)
   - Logging sanitization removes newlines from user input

**Security Patterns to Remember:**
- **Hardcoded secrets MUST be placeholders** in committed config files (appsettings.json, local.settings.json)
- **Auth middleware order:** UseAuthentication() before UseAuthorization(), UseRateLimiter() after UseAuthorization()
- **Session validation:** NEVER call SignOutAsync() from CookieValidatePrincipalContext — only RejectPrincipal()
- **Scope validation:** Every API endpoint needs HttpContext.VerifyUserHasAnyAcceptedScope()
- **Token refresh:** Save new tokens to Key Vault, log expiry timestamps only
- **SQL injection:** Use EF Core LINQ only, never FromSqlRaw with string concatenation
- **XSS protection:** Never use @Html.Raw unless output is sanitized; Razor auto-encodes @Model

**Actions Required:**
1. CRITICAL: Remove hardcoded App Insights key from Functions/local.settings.json
2. Review: LinkedIn/Index.cshtml displays access token in plaintext (consider masking)
3. Future: Document CORS policy for API (use explicit origins, never AllowAnyOrigin())

**Artifacts:**
- Full audit report: `.squad/decisions/inbox/oracle-security-audit-findings.md`


## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only
### 2026-04-07: GitHub Comment Formatting Skill Added
- Skill: .squad/skills/github-comment-formatting/SKILL.md now exists — canonical reference for formatting GitHub comments
- Rule: Use triple backticks for ALL fenced code blocks in GitHub content (PR reviews, issue comments, PR comments)
- Single backticks are for inline code only (single variable/method names, one line)
- Root cause of addition: PR #646 review used single-backtick fences; GitHub rendered broken inline code (words truncated, multi-line collapsed)
- Charter updated with enforcement rule (## How I Work)
- Read .squad/skills/github-comment-formatting/SKILL.md before posting any PR review or issue comment containing code