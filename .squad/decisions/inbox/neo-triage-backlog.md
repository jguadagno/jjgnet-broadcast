# Backlog Triage — Sprint 10 Assignment

**Date:** 2026-03-21  
**Decision maker:** Neo  
**Context:** Triage session for sprint:10 high/medium priority backlog

## Issues Closed

### #318 — feat(api): wire up granular OAuth2 scopes per API action
**Status:** CLOSED (resolved by PR #526)  
**Rationale:** PR #526 implemented fine-grained scopes (Resource.Action pattern: List, View, Modify, Delete) on all API endpoints with *.All fallback. Issue requirements fully met.

### #83, #85 — MSAL exceptions
**Status:** LEFT OPEN (partial resolution only)  
**Rationale:** PR #532 added `[AuthorizeForScopes]` to handle `MsalUiRequiredException` gracefully, but:
- #83 describes a cache collision error ("multiple tokens satisfying requirements") not directly addressed
- #85 includes an AADSTS650052 (app not subscribed to service) which is a configuration issue beyond code fixes

Both issues commented with partial resolution note; left open for further investigation.

## High-Priority Sprint 10 Assignments

| Issue | Title | Assigned | Rationale |
|-------|-------|----------|-----------|
| #307 | implement real calendar widget | **Sparks** | Razor views, Bootstrap theme, FullCalendar JS integration |
| #304 | add rate limiting to API | **Trinity** | API endpoints, ASP.NET Core middleware configuration |
| #301 | unit tests for publisher Functions | **Tank** | xUnit, Moq, FluentAssertions test coverage |
| #300 | unit tests for collector Functions | **Tank** | xUnit, Moq, FluentAssertions test coverage |

## Medium-Priority Sprint 10 Assignments

| Issue | Title | Assigned | Rationale |
|-------|-------|----------|-----------|
| #331 | remove SyndicationFeedReader network dependency | **Tank** | Unit testing with embedded XML/MemoryStream |
| #330 | add EngagementManager logic tests | **Tank** | xUnit tests for timezone correction, deduplication |
| #321 | cache Bluesky auth session | **Trinity** | Business logic, session management, DI architecture |

## Routing Decisions

**Tank workload:** 4 issues (all testing-related) — appropriate specialization  
**Trinity workload:** 2 issues (API + business logic) — appropriate specialization  
**Sparks workload:** 1 issue (web UI) — appropriate specialization  

**No issues assigned to Switch** (MVC controller layer) — current backlog is API/testing/UI-focused.

**No issues assigned `squad:joe`** per instructions — that label reserved for Joseph to self-assign.

## Sprint 10 Test Coverage Theme

With 4 out of 7 sprint:10 issues focused on testing (#300, #301, #330, #331), sprint 10 continues the test coverage expansion theme from sprint 9. This aligns with the roadmap goal of stabilizing Functions and Managers layers before expanding feature surface.
