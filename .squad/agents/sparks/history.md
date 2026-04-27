# Sparks — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-03-20 | Added CodeQL analysis to ci.yml (#326) | ✅ CodeQL job added as separate job with csharp language, push to main trigger added to workflow |
| 2026-04-03 | Implement health checks for Api and Web (#635) | ✅ Added SQL Server and Azure Storage health checks to ServiceDefaults; PR #641 created |
| 2026-05-02 | Schedule Add/Edit UI validation (#67) | ✅ Implemented ItemType dropdown and AJAX validation UI; branch feature/67-schedule-item-validation-ui pushed |
| 2026-04-11 | Fix double-submit bug in site.js (#708) | ✅ Added event.preventDefault() in submit handler to block duplicate form submissions; committed to branch social-media-708 |
| 2026-04-24 | Fix dt/dd HTML pairing in Schedules Details view (#845) | ✅ Changed all value `<dt>` elements to `<dd>` inside the `<dl class="row">` — 8 pairs now correctly use `<dt>` (label) + `<dd>` (value); PR #848 |
| 2026-04-11 | Fix AddPlatform 400 error (#708) | ✅ Removed redundant asp-route-engagementId from form causing model binding conflict; EngagementId now only posted via hidden field in ViewModel |

## Learnings

### 2026-04-27 — Issue #871: Engagements Index Column Headings Invisible
- **Root Cause:** `<thead class="thead-dark">` is a Bootstrap 4 class removed in Bootstrap 5. The sort link anchors inside `<th>` elements use `class="text-decoration-none text-white"`, so without a dark background the text is white-on-white — completely invisible.
- **Fix:** Changed `thead-dark` → `table-dark` (the Bootstrap 5 equivalent on `<thead>`).
- **Scope:** One-line change in `Views/Engagements/Index.cshtml`.
- **Pattern:** Any `thead-dark` in the codebase is a Bootstrap 4 leftover. Other index views (Schedules, YouTubeSources, etc.) also use `thead-dark` but lack `text-white` links so they still render (black text on default white background). Those are cosmetic regressions (missing dark header), not visibility blockers.
- **Branch:** `issue-871-fix-engagements-column-headings` | **PR:** #874

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

### 2026-05-02: Schedule Add/Edit AJAX Validation (Issue #67)
- **Integration with Backend:** Trinity implemented `ValidateItem` endpoint returning JSON (`isValid`, `itemTitle`, `itemDetails`, `errorMessage`)
- **Enum Dropdown Pattern:** Use `asp-items="Html.GetEnumSelectList<EnumType>()"` for enum-based dropdowns; auto-generates options with proper value binding
- **Bootstrap 5 Input Groups:** Use `input-group` class to attach buttons to input fields; `btn-outline-secondary` for non-primary action buttons
- **AJAX Validation UX:** Show loading spinner during request (`spinner-border spinner-border-sm`), replace with success/error/warning alert on completion
- **Bootstrap Icons:** Use `bi-check-circle-fill` (success), `bi-x-circle-fill` (error), `bi-exclamation-triangle-fill` (warning) for visual feedback
- **Backward Compatibility:** Keep hidden `ItemTableName` field synced from `ItemType` enum via JS mapping for legacy code compatibility
- **Form Enhancement:** Support Enter key in input field to trigger validation (prevent default form submit, call validation function)
- **jQuery AJAX Pattern:** Standard pattern: show loading → call endpoint → handle success/error → display feedback in result div
- **Build Time:** Web project build takes ~160s with 87 pre-existing nullable warnings (CS8618 — standard pattern in this codebase)

### 2026-04-08 — Epic #667 Assigned: Social Media Platforms (Razor Views)
- **Task:** Razor views for managing platform associations on Engagements/Talks (Add/Edit/List)
- **Dependency:** Switch controller work must complete first
- **Status:** 🔴 BLOCKED — waiting on Switch → Trinity → Morpheus → Joseph's answers
- **Triage source:** Neo (issue #667)


### 2026-04-08 — Epic #667 Architecture Decisions Resolved
- **Status change:** 🟡 WAITING ON SWITCH (unblocked from Joseph's answers)
- **Key decisions affecting Sparks (Razor Views):**
  - Views needed: SocialMediaPlatforms list/create/edit (admin); EngagementSocialMediaPlatforms add/remove on Engagement detail
  - IsActive shown as ✗ icon; list page has toggle button to flip IsActive
  - Platform dropdowns on ScheduledItems and MessageTemplates views (replace free-text with FK dropdown)
- **Next:** Begin Razor views after Switch delivers controller layer

### 2026-04-11 — Issue #708: Double-Submit Bug Fix
- **Root Cause:** In site.js, the global form submit handler checked `if (btn.disabled) return;` but did not call `event.preventDefault()` when the button was already disabled
- **Impact:** Fast double-click sent duplicate POST requests, causing "duplicate platform add" errors in AddPlatform flow
- **Fix:** Added `event` parameter to submit handler and call `event.preventDefault()` before returning when button is disabled
- **File Changed:** `JosephGuadagno.Broadcasting.Web/wwwroot/js/site.js` (lines 8-12)
- **Pattern:** Always accept the `event` parameter in event listeners and call `preventDefault()` when blocking default behavior
- **Branch:** social-media-708 (existing branch for this fix)
- **Commit:** 079cb14
- **Regression Coverage:** Backend API validation prevents data corruption even if double-submit occurs (15 tests already passing); no new test framework added
- **Status:** ✅ Complete — Orchestration log 2026-04-11T22-34-33Z-sparks.md recorded; decisions merged to decisions.md

### 2026-04-11 — Issue #708: AddPlatform 400 Error (Route Parameter Conflict)
- **Root Cause:** The AddPlatform.cshtml form had both `asp-route-engagementId="@Model.EngagementId"` in the form action AND a hidden field `<input asp-for="EngagementId" />`, causing ASP.NET Core model binding confusion
- **Impact:** POST to AddPlatform returned HTTP 400 Bad Request because the controller action signature `AddPlatform(int engagementId, EngagementSocialMediaPlatformViewModel vm)` couldn't resolve whether `engagementId` should come from the route or the model property
- **Fix:** Removed `asp-route-engagementId` attribute from the form tag; `EngagementId` is now posted solely via the hidden field as part of the ViewModel
- **File Changed:** `JosephGuadagno.Broadcasting.Web/Views/Engagements/AddPlatform.cshtml` (line 11)
- **Pattern:** When a controller action accepts both a route parameter AND a model with a matching property name, avoid duplicating the value in both the route and the form — choose one binding source (prefer model binding for forms)
- **Branch:** social-media-708
- **Commit:** ce28027
- **Status:** ✅ Complete

## Session Complete: Issue #708 Final Trace (2026-04-11)

- **Work:** Scribe session to consolidate Sparks' Issue #708 fixes and team decisions
- **Orchestration log:** `.squad/orchestration-log/2026-04-11T22-51-44Z-sparks.md` — Captured both fixes (site.js + AddPlatform.cshtml)
- **Session log:** `.squad/log/2026-04-11T22-51-44Z-issue-708-form-trace.md` — Issue summary and pattern documented
- **Decision merged:** `sparks-708-form-route-binding.md` → decisions.md (model binding pattern established for team)
- **Outcome:** Issue #708 fully resolved; dual fixes committed (079cb14, ce28027); team pattern documented for future form implementations

### 2026-04-11 — Issue #708 REVERSAL: Route Parameter Required (Second Investigation)
- **Root Cause:** Previous fix (commit ce28027) that removed `asp-route-engagementId` from AddPlatform form was INCORRECT
- **Impact:** Without route parameter, form POSTs to `/Engagements/AddPlatform` (no engagementId), causing HTTP 400 because controller action signature `AddPlatform(int engagementId, EngagementSocialMediaPlatformViewModel vm)` expects engagementId as a ROUTE parameter
- **Fix:** Restored `asp-route-engagementId="@Model.EngagementId"` to form action — engagementId is now passed in BOTH route AND hidden field (not a conflict when both sources provide same value)
- **File Changed:** `JosephGuadagno.Broadcasting.Web/Views/Engagements/AddPlatform.cshtml` (line 11)
- **Pattern Correction:** Controller actions with simple-type route parameters (int, string) MUST have those values in the route. Hidden fields in the ViewModel are for model binding, not routing. When an action expects `AddPlatform(int engagementId, ModelType vm)`, the route must include engagementId
- **Branch:** social-media-708
- **Commit:** 2fa1fe2
- **Status:** ✅ Complete — Previous decision sparks-708-form-route-binding.md was INCORRECT and should be superseded by this learning

## Session 2026-04-11T23:23:33Z — Orchestration Closure

**Scribe** consolidated Issue #708 resolution:
- Orchestration log: `.squad/orchestration-log/2026-04-11T23-23-33Z-Sparks.md` — Final fix documented (asp-route-engagementId restored)
- Session log: `.squad/log/2026-04-11T23-23-33Z-issue-708-save-400-web.md` — Web form routing fix logged
- Status: ✅ Complete — Issue #708 fully resolved; Sparks' all fixes committed and documented

### 2026-05-02 — Issue #814: Help Pages — Social Media Credential Setup
- **Branch:** `issue-814-help-pages` | **PR:** #841
- **Controller:** `HelpController` with `[Authorize]` and `[Route("Help/SocialMediaPlatforms/{platform}")]` action
- **Route pattern:** Default MVC route is `{controller}/{action}/{id?}` — parameter name mismatch (`platform` vs `id`) required an explicit `[Route]` attribute on the action
- **View resolution:** Because views live in a subdirectory (`Views/Help/SocialMediaPlatforms/`), must pass the relative sub-path to `View()` explicitly: `View("SocialMediaPlatforms/LinkedIn")` not `View("linkedin")`
- **Platform → view name mapping:** Used a `Dictionary<string, string>` (case-insensitive) to map slug → exact view path; avoids issues with mixed casing (e.g., "LinkedIn" vs "linkedin")
- **Views created:** Bluesky, Twitter, LinkedIn, Facebook, Mastodon — all in `Views/Help/SocialMediaPlatforms/`
- **Layout:** Each view inherits the shared `_Layout.cshtml` via `_ViewStart.cshtml` (no explicit Layout assignment needed)
- **Pattern:** Bootstrap 5 card layout with breadcrumb, credential table, step-by-step ordered list, and official docs link that opens in a new tab
- **Unknown platform slug:** Returns HTTP 404 (dictionary miss)
- **Status:** ✅ Complete — Build green, tests green, PR #841 open

### 2026-04-24 — Issue #845: dt/dd HTML Pairing Fix
- **File:** `Views/Schedules/Details.cshtml`
- **Problem:** All 8 value cells in the `<dl class="row">` were using `<dt>` instead of `<dd>`. Every row had two `<dt>` elements (label + value), leaving no `<dd>` elements at all.
- **Fix:** Changed the value element in each row from `<dt class="col-sm-9">` to `<dd class="col-sm-9">`. Bootstrap column classes were preserved unchanged.
- **Pattern:** In Bootstrap 5 description lists using `<dl class="row">`, label cells use `<dt class="col-sm-N">` and value cells use `<dd class="col-sm-N">`. Never use two `<dt>` elements in the same row.
- **Branch:** `issue-845-code-quality-cleanup` | **PR:** #848
