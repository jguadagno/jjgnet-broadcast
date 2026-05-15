# Sparks — History

## Summary

Sparks (Frontend/Polish Specialist) focuses on UI/UX refinements, Bootstrap 5 migration, feature polish, and infrastructure improvements. Work includes CodeQL CI integration, health checks for API/Web services, HTML semantic fixes (dt/dd pairing), AJAX validation UI, and index page sort/filter/search enhancements. Key patterns: filter form persistence (preserve filter/sort state across page interactions via asp-route-* params), Bootstrap 5 class updates (thead-dark → table-dark on thead elements), explicit ViewBag casting in Razor (cast to specific type before foreach), enum dropdown patterns with auto-submit (select onchange="this.form.submit()"), and w-auto for compact dropdown sizing. Established GitHub comment formatting guidelines (triple backticks for all fenced code blocks). Works closely with Switch (form UI), Trinity (API filtering/sorting wiring), and Tank (health check tests). Notable: Sparks identifies and fixes regressions from library updates (Bootstrap 4→5), adds infrastructure resilience features (health checks), and improves developer workflow (CodeQL). Key decision: sort/filter must wire all the way to service/API layer, not just UI. Pattern: any visible regression in index page styling is likely Bootstrap 4 leftover that needs migration (e.g., thead-dark). Important: Always preserve all active filter params in sort column links to maintain user's filter state.

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-05-12 | Refactor CollectorSettings/Index.cshtml — remove modals, add SpeakingEngagements section (#950) | ✅ Replaced all modal Add/Edit/Delete triggers with redirect links; added Speaking Engagements card; applied table-dark to all section tables; removed modal divs and JS scripts; build succeeded 0 errors |
| 2026-03-20 | Added CodeQL analysis to ci.yml (#326) | ✅ CodeQL job added as separate job with csharp language, push to main trigger added to workflow |
| 2026-04-03 | Implement health checks for Api and Web (#635) | ✅ Added SQL Server and Azure Storage health checks to ServiceDefaults; PR #641 created |
| 2026-05-02 | Schedule Add/Edit UI validation (#67) | ✅ Implemented ItemType dropdown and AJAX validation UI; branch feature/67-schedule-item-validation-ui pushed |
| 2026-04-11 | Fix double-submit bug in site.js (#708) | ✅ Added event.preventDefault() in submit handler to block duplicate form submissions; committed to branch social-media-708 |
| 2026-04-24 | Fix dt/dd HTML pairing in Schedules Details view (#845) | ✅ Changed all value `<dt>` elements to `<dd>` inside the `<dl class="row">` — 8 pairs now correctly use `<dt>` (label) + `<dd>` (value); PR #848 |
| 2026-04-11 | Fix AddPlatform 400 error (#708) | ✅ Removed redundant asp-route-engagementId from form causing model binding conflict; EngagementId now only posted via hidden field in ViewModel |
| 2026-04-27 | Add sorting, filtering, searching to all index pages (#870) | ✅ Schedules index full sort/filter/H1/Bootstrap 5 fix; Engagements index H1; SyndicationFeedSources + YouTubeSources thead-dark → table-dark; wired Schedules service sort/filter to API; PR #876 |
| 2026-05-14 | Refactor MessageTemplates GetSocialIcon helper (#950) | ✅ Removed @functions block; injected ISocialMediaPlatformService into controller; built ViewBag.PlatformIcons dictionary with "bi-broadcast" fallback; updated view to use dictionary lookup; build 0 errors; branch issue-950-sanity-check |

## Learnings

### 2026-05-14 — MessageTemplates: Replace GetSocialIcon with SocialMediaPlatform.Icon
- **Files:** `Controllers/MessageTemplatesController.cs`, `Views/MessageTemplates/Index.cshtml`
- **Change:** Removed the hardcoded `@functions { GetSocialIcon(string platform) }` helper from the view. Injected `ISocialMediaPlatformService` as a third constructor parameter in the controller. In `Index()`, called `GetAllAsync(pageSize: 100)` to build a `Dictionary<string, string>` keyed on `platform.Name` with value `platform.Icon ?? "bi-broadcast"`, stored in `ViewBag.PlatformIcons`.
- **View pattern:** Cast `ViewBag.PlatformIcons` once before the `@foreach` loop (`var platformIcons = (Dictionary<string, string>)ViewBag.PlatformIcons;`), then use `TryGetValue` inside the loop with `??= "bi-broadcast"` null-coalesce fallback. No inline `@{ }` block needed inside the loop — Razor executes inline C# statements directly in loop body.
- **Key rule:** `SocialMediaPlatform.Icon` is `string?` — always apply `?? "bi-broadcast"` fallback both when building the dictionary (controller) and when resolving the value (view).
- **Branch:** `issue-950-sanity-check`

### 2026-05-12 — CollectorSettings Modal → Redirect Refactor
- **Pattern:** When multiple inline modals (Add + Edit per entity type) share a single settings page, replace them with redirect links to dedicated controller actions. Modals require data-attribute state transfer and JS listeners; dedicated pages get clean model binding, asp-validation-for spans, and server-side validation for free.
- **thead class:** All section tables on settings pages should use `<thead class="table-dark">` — consistent with Bootstrap 5 charter and other index pages.
- **CollectorFeedSources views:** Were already present on the branch when Sparks arrived — verify existing work before creating new files.
- **SpeakingEngagements section icon:** `bi-mic-fill` used for speaking engagements card header.

### 2026-05-02 — MessageTemplates Platform Filter Dropdown
- **File:** `Views/MessageTemplates/Index.cshtml`
- **Change:** Added `<select name="selectedPlatform">` dropdown between text filter and Filter button; populated from `(List<string>)ViewBag.Platforms` with explicit cast required in Razor.
- **Auto-submit pattern:** `onchange="this.form.submit()"` on the select enables immediate filtering without clicking the Filter button — good UX for low-option dropdowns.
- **w-auto:** Use `form-select w-auto` to keep select width compact in a flex row; without `w-auto` it stretches to fill available space.
- **Clear button pattern:** Always include ALL active filter params in the `@if` condition: `!string.IsNullOrEmpty(ViewBag.Filter as string) || !string.IsNullOrEmpty(ViewBag.SelectedPlatform as string)`.
- **Sort link preservation:** Every `asp-route-*` param in sort column links must include all active filter state (`asp-route-filter`, `asp-route-selectedPlatform`) so platform filter survives sort clicks.
- **ViewBag cast rule:** `ViewBag.Platforms` is `dynamic`; always cast explicitly in Razor: `(List<string>)ViewBag.Platforms`. Without the cast the foreach fails at runtime.

### 2026-04-27 — Issue #870: Sorting/Filtering/Searching on All Index Pages
- **Pattern established:** All index pages use: filter `<form method="get">` with hidden `sortBy`/`sortDescending` inputs, sortable `<thead class="table-dark">` column headers with `asp-route-sortBy/sortDescending/filter`, and `<partial name="_PaginationPartial" />` at bottom.
- **Bootstrap 5 fix sweep:** `thead-dark` (Bootstrap 4) → `table-dark` (Bootstrap 5) on `<thead>`. Found in Schedules, SyndicationFeedSources, YouTubeSources. Engagements was fixed in PR #874.
- **H1 requirement:** Issue #870 explicitly requires every index page to have `<h1>PageName</h1>`. Engagements was missing one; Schedules was missing one.
- **Data layer rule applies:** Sort/filter wiring must go all the way to the service/API. For Schedules, `IScheduledItemService.GetScheduledItemsAsync` was updated with `sortBy`, `sortDescending`, `filter` params. The API (`GET /Schedules`) already supported these params.
- **Non-applicable pages:** CollectorSettings (card/modal per-user settings), PublisherSettings (card-based per-platform view), MessageTemplates (platform-grouped admin view — `IMessageTemplateService` has no filter param), Home/LinkedIn (non-list pages).
- **Branch:** `issue-870-sorting-filtering-searching-index-pages` | **PR:** #876
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

