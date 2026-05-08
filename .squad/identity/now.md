# Team Focus — Now

> **Last updated:** 2026-05-08T16:15:00Z
> **Sprint:** 31 — Milestone 27
> **Status:** Sprint 30 CLOSED (ISocialMediaPublisher #897 complete; composition refactor #902 merged). Sprint 31 ACTIVE — caching enhancements. #803 COMPLETE (PR #934 merged to main).
> **Next:** Sprint 31 caching work: #935 (P3, PR #938 open), #936 (P1), #937 (P2) under way; #933 (speaking engagement functions) in progress; #930 (NuGet deprecation) queued.

## Current Focus

**Sprint 30 — COMPLETE** ✅
- ✅ #897 — Define ISocialMediaPublisher common interface (COMPLETE — test coverage finalized, DI wiring verified)
- ✅ #902 — Move LinkedIn message composition to LinkedInManager (COMPLETE — merged to main)

**Sprint 31 — ACTIVE** 🔄
- ✅ #803 — Allow `.squad` updates to commit from main (COMPLETE — PR #934 merged to main)
- 🔜 #78 — Add caching to WebApi (parent issue)
  - 🔜 #935 — Add caching to SyndicationFeedSourceManager and YouTubeSourceManager (P3, PR #938 open)
  - 🔜 #936 — Add caching to MessageTemplateManager (P1)
  - 🔜 #937 — Add user-scoped caching to Engagements, Schedules, UserPublisherSettings managers (P2)
- 🔜 #933 — Create Azure Functions for handling new speaking engagements (Trinity agent running)
- ⏳ #930 — NuGet package AspNetCore.HealthChecks.AzureStorage has been deprecated (queued for Trinity)

**Goal:** Sprint 31 delivers #803 completion (PR #934 merged) + caching phase 1 (SyndicationFeed/YouTube, MessageTemplate, user-scoped managers).

## Key Patterns (Sprint 31)

1. **Dual-key caching:** Global + user-scoped `IMemoryCache` with 5-minute absolute expiry
2. **Cache invalidation:** Remove both keys on Save/Delete(entity); global key only on Delete(id)
3. **Stale tolerance:** Up to 5 minutes for user-scoped entries when Delete(id) only available

## Standing Work

- #78: Caching enhancements (parent issue, Sprint 31)
- #933: Speaking engagement functions (Trinity, in progress)
- #930: NuGet deprecation (queued for Trinity)
- #724: Multi-user teams/groups — Deferred pending explicit Joe confirmation that #609 is 100% production-ready

## Team Composition

**Sprint 31 (Active):**
- #78 / #935–#937 — Caching work (Trinity)
- #933 — Speaking engagements (Trinity agent)
- #930 — NuGet deprecation (queued)
- Joe — Parallel infrastructure tasks

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-05-08T16:15:00Z
**Sprint:** 31 (Caching enhancements + housekeeping PR bypass) — #803 COMPLETE, #78 in progress
**Current Focus:** #935 (P3) PR #938 open; #936 (P1) and #937 (P2) in progress
**Next Decision Point:** #935–#937 completion → Sprint 32 readiness
