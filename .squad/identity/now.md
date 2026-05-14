# Team Focus — Now

> **Last updated:** 2026-05-14T06:17:02Z
> **Sprint:** 31 — Milestone 27
> **Status:** Sprint 30 CLOSED (ISocialMediaPublisher #897 complete; composition refactor #902 merged). Sprint 31 ACTIVE — MessageTemplate refactor. #930 #933 #935 #936 #937 COMPLETE.
> **Current branch:** `issue-950-sanity-check` — Refactor in `MessageTemplates\Index.cshtml` replacing `GetSocialIcon()` helper with `SocialMediaPlatform.Icon` (Sparks in progress).

## Current Focus

**Sprint 30 — COMPLETE** ✅
- ✅ #897 — Define ISocialMediaPublisher common interface (COMPLETE — test coverage finalized, DI wiring verified)
- ✅ #902 — Move LinkedIn message composition to LinkedInManager (COMPLETE — merged to main)

**Sprint 31 — ACTIVE** 🔄
- ✅ #803 — Allow `.squad` updates to commit from main (COMPLETE — PR #934 merged to main)
- ✅ #930 — NuGet package AspNetCore.HealthChecks.AzureStorage deprecation (COMPLETE)
- ✅ #933 — Create Azure Functions for handling new speaking engagements (COMPLETE)
- ✅ #935 — Add caching to SyndicationFeedSourceManager and YouTubeSourceManager (COMPLETE — PR #938 merged)
- ✅ #936 — Add caching to MessageTemplateManager (COMPLETE)
- ✅ #937 — Add user-scoped caching to Engagements, Schedules, UserPublisherSettings managers (COMPLETE)
- 🔄 #950 — MessageTemplate refactor (Sparks in progress on `issue-950-sanity-check` branch)
  - Replacing `GetSocialIcon()` helper with `SocialMediaPlatform.Icon` from database in `MessageTemplates\Index.cshtml`

**Goal:** Sprint 31 delivers #803 completion (PR #934 merged) + caching phase 1 (SyndicationFeed/YouTube, MessageTemplate, user-scoped managers).

## Key Patterns (Sprint 31)

1. **Dual-key caching:** Global + user-scoped `IMemoryCache` with 5-minute absolute expiry
2. **Cache invalidation:** Remove both keys on Save/Delete(entity); global key only on Delete(id)
3. **Stale tolerance:** Up to 5 minutes for user-scoped entries when Delete(id) only available

## Standing Work

- #950: MessageTemplate refactor (Sparks, in progress, `issue-950-sanity-check`)
- #724: Multi-user teams/groups — Deferred pending explicit Joe confirmation that #609 is 100% production-ready

## Team Composition

**Sprint 31 (Active):**
- #950 — MessageTemplate refactor (Sparks, `issue-950-sanity-check`)
- Joe — Parallel infrastructure tasks

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-05-14T06:17:02Z
**Sprint:** 31 (Caching + MessageTemplate refactor) — #930 #933 #935 #936 #937 COMPLETE
**Current Focus:** #950 MessageTemplate refactor in progress on `issue-950-sanity-check`
**Next Decision Point:** #950 completion → Sprint 32 readiness
