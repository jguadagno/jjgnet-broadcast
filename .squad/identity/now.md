# Team Focus — Now

> **Last updated:** 2026-04-24T21:24:52Z
> **Sprint:** 27 (complete) — Milestone 21
> **Status:** ✅ RESOLVED. Issue #858 (manual DB migration) completed by Joe in production. No blocking work remains for Sprint 27.
> **Next:** Sprint 28 kickoff or continue with open backlog issues.

## Current Focus

**Sprint 27 — ALL DELIVERABLES COMPLETE** ✅

**Core Deliverables:** ✅ COMPLETE
- ✅ #777 — Per-user OAuth/token runtime (merged via PR #854)
- ✅ #778 — Per-user collector onboarding/configuration (merged via PR #859)
- ✅ #858 — Manual DB migration for per-user collector config tables (Joe completed in production 2026-04-24T21:24:52Z)

**Goal:** Begin Sprint 28 planning or complete remaining backlog issues. Monitor CodeQL CSRF alert #41 for next scan resolution.

## Key Patterns (Sprint 27)

1. **Per-user OAuth:** `LinkedinController` (and other platforms) acquire and store tokens per user.
2. **Per-user collectors:** `UserCollectors` data model; queries filter by current user's OID — no cross-user leakage.
3. **Security invariant:** Tests prove a user cannot read or execute another user's tokens or collector configurations.

## Standing Work

- #803: Allow `.squad` updates to commit from main (squad:neo, low priority)
- #724: Multi-user teams/groups — Epic #609 now complete, ready for implementation
- CodeQL CSRF alert #41 — monitoring, may resolve on next scan

## Team Composition

**Sprint 27 (Complete):**
- Trinity — Implementation (#777, #778) ✅
- Tank — Test coverage ✅
- Neo — Review & architecture ✅

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-04-24T21:24:52Z
**Sprint:** 27 (Multi-Tenancy: Per-User OAuth & Collectors) — COMPLETE
**Current Focus:** Sprint planning or backlog
**Next Decision Point:** Sprint 28 kickoff
