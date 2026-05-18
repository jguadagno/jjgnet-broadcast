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

### 2026-05-16: YouTube Key Vault Secret Name Underscore Violation

**Context:** Users saving a YouTube Channel collector caused Key Vault secret creation to fail with a naming violation. The secret name contained underscores because the `discriminator` parameter (the YouTube Channel ID, e.g. `UC_my_channel`) was inserted raw into the secret name without sanitization.

**Root Cause:** `KeyVaultSecretNameBuilder.Build()` applied `SecretNameSanitizer` only to `ownerOid`. The `discriminator` parameter — a user-supplied YouTube Channel ID — was concatenated directly into the name string, bypassing all sanitization.

**Fix:** Extracted a `SanitizeSegment()` private helper from the existing compiled regex (`[^a-zA-Z0-9\-]` → `"-"`) and applied it to **all** string components: `ownerOid`, `platform`, `settingName`, and `discriminator`. The `platform` and `settingName` parameters are currently always passed as `KeyVaultSecretNames` constants (already clean), but sanitizing them is a required defence-in-depth guard.

**Tests Added:** `Build_WithSpecialCharsInDiscriminator_SanitizesToHyphens` (3 cases covering `_`, mixed `_` with digits, and `@#!` characters).

**Security Rule to Remember:**
- **Every component that feeds into a Key Vault secret name must pass through `SanitizeSegment()`** before concatenation — no exceptions, even for "constant" values.
- YouTube Channel IDs are user-supplied and CAN contain underscores or other special characters; never trust them as pre-sanitized.

**Commit:** `fix(keyvault): sanitize underscores in Key Vault secret names` (branch: issue-972-end-user-validation)

### 2026-05-16: YouTube Channel ID Collision — Sanitization Is Insufficient, Must Hash

**Context:** The previous fix (above) replaced `_` and other special chars with `-` in the discriminator segment. Joe identified that this creates a **silent collision**: YouTube Channel IDs use base64url encoding where both `-` and `_` are valid, meaningful characters. Two distinct channel IDs (`UCabc-def` and `UCabc_def`) would sanitize to the same name, causing one user's Key Vault secret to silently overwrite another's.

**Root Cause:** Simple character-substitution sanitization is not collision-safe for base64url-encoded identifiers. It only ensures the output is Key-Vault-safe, not that distinct inputs produce distinct outputs.

**Fix:** Replaced `SanitizeSegment(discriminator)` with `HashDiscriminator(discriminator)` in `KeyVaultSecretNameBuilder.Build()`. `HashDiscriminator()` computes the SHA-256 hash of the discriminator (UTF-8), takes the first 8 bytes, and returns 16 lowercase hex characters. Properties:
- **Deterministic:** Same channel ID → always same hash → always same Key Vault name.
- **Collision-resistant:** SHA-256 truncated to 64 bits — astronomically unlikely to collide for any realistic user-set size.
- **No character restrictions:** Any Unicode input maps to a safe `[a-z0-9]` hex string.
- **Key Vault compliant:** 16 hex chars fits easily within the 127-char name limit.

`SanitizeSegment()` is retained for `ownerOid`, `platform`, and `settingName` — those are controlled values not subject to base64url collision risk.

**Security Rule to Remember:**
- **Sanitization alone is not safe for user-supplied discriminators that use base64url encoding.** When the identifier space includes both `-` and `_` as semantically distinct characters, you MUST hash to avoid silent collisions.
- SHA-256 (first 8 bytes, hex) is the standard approach for turning arbitrary identifiers into short, safe, collision-resistant Key Vault name segments.

**Tests Updated:**
- Removed `Build_WithSpecialCharsInDiscriminator_SanitizesToHyphens` (wrong premise).
- Added `Build_WithUnderscoreDiscriminator_ProducesConsistentHash` — proves `UCabc_def` ≠ `UCabc-def` in output.
- Added `Build_WithDiscriminator_OutputContainsOnlyLowercaseHexInDiscriminatorSegment` — proves all output chars are `[a-z0-9-]`.
- Updated `Build_WithDiscriminator_InsertsHashedDiscriminatorBetweenPlatformAndSettingName` with correct hash-based expected values.

**Commit:** `fix(keyvault): use SHA-256 hash for discriminator in secret names to prevent base64url collisions` (branch: issue-972-end-user-validation)

### 2026-05-16: Session Close — Decisions Archive Established

**Context:** Scribe processed 3 inbox decision files from issue-972-end-user-validation and merged them into decisions.md. Orchestration logs created for Oracle and Sparks agents. All team updates recorded in cross-agent history.

**Actions Taken:**
1. Merged oracle-keyvault-secret-name-sanitization.md → decisions.md (Hash Discriminator decision)
2. Merged sparks-filter-button-loading-text.md → decisions.md (Form loading text decision)
3. Merged sparks-localhost-url-validation.md → decisions.md (Development URL validation decision)
4. Deleted all 3 inbox files
5. Created orchestration logs for Oracle and Sparks
6. No history files required summarization (all < 15360 bytes)

**Status:** All session artifacts committed to .squad/. Ready for next sprint.