# Switch — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Frontend Engineer
- **Joined:** 2026-03-14T16:55:20.779Z

## Learnings

### 2026-03-16: Issue #105 - Conference Social Fields UI
- Added `ConferenceHashtag` and `ConferenceTwitterHandle` fields to Engagement Create/Edit/Details views
- These fields are nullable `string?` properties added to `EngagementViewModel` in PR #529
- Form pattern: Bootstrap `mb-3` div with label, input, validation span using `asp-for` helpers
- Details pattern: Show only if `!string.IsNullOrEmpty()` to avoid displaying empty rows
- **CRITICAL**: In Razor views, escape `@` in HTML attributes with `@@` (e.g., `placeholder="@@MyConference"`)
- PR #534 created, builds successfully with 0 errors

### 2026-03-20T20:11:20Z — Orchestration Log & Session Completion
- **Task:** Record engagement social fields Web UI completion
- **Orchestration log:** Created 2026-03-20T20-11-20Z-switch.md documenting PR #534 (Engagement Create/Edit/Details views with ConferenceHashtag and ConferenceTwitterHandle)
- **Build status:** Clean build, 0 errors
- **PR status:** #534 open, ready for review
- **Vertical slice completion:** Full engagement social fields feature now spans Domain → Data → API → Web UI (all layers in sync)
- **Pattern documented:** Form accessibility improvements (PR #522) blocked on ViewModel updates, to be rebase once BlueSkyHandle props added


### 2026-04-01 — Issue Spec #573 (Web paging UI)
- **Relevant specs:** `.squad/sessions/issue-specs-591-575-574-573.md`
- **Issue #573** — Add paging UI to Web controllers and views. Pass paging metadata via `ViewBag` (Decision D4). Do not change existing `@model List<T>` view types. Shared `_PaginationPartial.cshtml` reads from `ViewBag`.
- **Dependency:** Blocked on Trinity completing #574 API layer work.


## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only