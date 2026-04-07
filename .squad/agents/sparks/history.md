# Sparks — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-03-20 | Added CodeQL analysis to ci.yml (#326) | ✅ CodeQL job added as separate job with csharp language, push to main trigger added to workflow |
| 2026-04-03 | Implement health checks for Api and Web (#635) | ✅ Added SQL Server and Azure Storage health checks to ServiceDefaults; PR #641 created |

## Learnings

### Health Checks in ServiceDefaults (2026-04-03)
- ServiceDefaults uses `IHostApplicationBuilder` where `builder.Configuration` is `IConfigurationManager` (not `IConfiguration`)
- Connection strings must be accessed via indexer: `builder.Configuration["ConnectionStrings:KeyName"]` not `GetConnectionString()`
- Health checks should be conditionally registered with null/whitespace checks to keep ServiceDefaults safe for any consumer
- SQL Server check package: `AspNetCore.HealthChecks.SqlServer` version 9.0.0 (compatible with .NET 10)
- Azure Storage check package: `AspNetCore.HealthChecks.AzureStorage` version 7.0.0 (latest available; v9 not yet released)
- Connection string keys: `JJGNetDatabaseSqlServer` (SQL), `QueueStorage` (Azure Storage queues)
- Health check tags: `["live"]` for liveness (self-check only), `["ready"]` for readiness (includes dependencies)
- Endpoints: `/alive` (liveness), `/health` (readiness) — already mapped in ServiceDefaults

### CodeQL Integration in CI
- CodeQL works best as a separate job with its own permissions (security-events: write)
- For .NET 10 preview, match the dotnet-quality: 'preview' setting from existing jobs
- CodeQL requires a build step for compiled languages like C#; used `dotnet build src/ --no-incremental`
- Added push to main trigger to ensure CodeQL runs on both PRs and main branch commits
- Vulnerable package scanning was already implemented with Critical CVE failure threshold


### 2026-04-01 — Issue Spec #573 (Web paging UI — frontend)
- **Relevant specs:** `.squad/sessions/issue-specs-591-575-574-573.md`
- **Issue #573** — Paging controls need frontend wiring. Shared `_PaginationPartial.cshtml` partial view to be created/updated to render page navigation using `ViewBag.Page`, `ViewBag.TotalPages` etc.
- **Dependency:** Blocked on Trinity completing #574 API layer work.


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