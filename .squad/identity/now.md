# Team Focus — Now

> **Last updated:** 2026-05-14T06:20:22Z
> **Sprint:** 31 — Sanity Check / Stabilization Phase
> **Status:** Paused new issue intake. Testing and validating recent work from past weeks. Fixing critical regressions; filing non-critical findings as GitHub issues.
> **Current branch:** `issue-950-sanity-check` — MessageTemplate refactor in `MessageTemplates\Index.cshtml` replacing `GetSocialIcon()` helper with `SocialMediaPlatform.Icon` (Sparks completing). **No PR creation unless Joe explicitly requests.**

## Current Focus

**Sanity Check / Stabilization Sprint** 🔍

- **Mode:** Test and validate all recent work from the past few weeks
- **Goal:** Identify critical regressions; fix immediately. Non-critical findings filed as GitHub issues, not fixed in-sprint.
- **Active work:** `issue-950-sanity-check` branch — Sparks completing the `GetSocialIcon()` → `SocialMediaPlatform.Icon` refactor in `MessageTemplates\Index.cshtml`
- **Blocked on:** Not starting new issue work until codebase is safe to deploy
- **Deployment readiness:** No PR creation for sanity-check branch work unless Joe explicitly requests

## Recent Sprint Completions (Pre-Sanity Check)

**Sprint 30 — COMPLETE** ✅
- ✅ #897 — Define ISocialMediaPublisher common interface
- ✅ #902 — Move LinkedIn message composition to LinkedInManager

**Sprint 31 — COMPLETE** ✅
- ✅ #803 — Allow `.squad` updates to commit from main
- ✅ #930 — NuGet package AspNetCore.HealthChecks.AzureStorage deprecation
- ✅ #933 — Create Azure Functions for handling new speaking engagements
- ✅ #935 — Add caching to SyndicationFeedSourceManager and YouTubeSourceManager
- ✅ #936 — Add caching to MessageTemplateManager
- ✅ #937 — Add user-scoped caching to Engagements, Schedules, UserPublisherSettings managers
- 🔄 #950 — MessageTemplate refactor (Sparks, `issue-950-sanity-check`, completing)

## Key Patterns (from Recent Work)

1. **Dual-key caching:** Global + user-scoped `IMemoryCache` with 5-minute absolute expiry
2. **Cache invalidation:** Remove both keys on Save/Delete(entity); global key only on Delete(id)
3. **Stale tolerance:** Up to 5 minutes for user-scoped entries when Delete(id) only available

## Standing Work

- #950: MessageTemplate refactor (Sparks, in progress, `issue-950-sanity-check`)
- #724: Multi-user teams/groups — Deferred pending explicit Joe confirmation that #609 is 100% production-ready

## Team Composition (Sanity Check Phase)

- **Joe** — Testing, validation, triage, deployment readiness
- **Sparks** — Completing #950 MessageTemplate refactor on `issue-950-sanity-check`

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-05-14T06:20:22Z
**Phase:** Sanity Check / Stabilization Sprint
**Current Focus:** Validating all recent work; no new issue intake until codebase is deployment-ready
**Next Decision Point:** Sanity check complete → Sprint 32 planning + backlog readiness
