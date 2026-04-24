# Team Focus — Now

> **Last updated:** 2026-04-24T21:25:03Z
> **Sprint:** 27 (active) — Milestone 21
> **Status:** Sprint 27 core deliverables complete. PR #859 (#778) merged. Focused on completing remaining open issues.
> **Next:** Close open issues from backlog. CodeQL CSRF alert #41 pending next scan.

## Current Focus

**Sprint 27 — Completing Remaining Open Issues (Epic #609 Complete)**

**Core Deliverables:** ✅ COMPLETE
- ✅ #777 — Per-user OAuth/token runtime (merged via PR #854)
- ✅ #778 — Per-user collector onboarding/configuration (merged via PR #859)

**Goal:** Complete remaining open issues from the backlog. Monitor CodeQL CSRF alert #41 for next scan resolution.

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

**Last Updated:** 2026-04-24T21:25:03Z
**Sprint:** 27 (Multi-Tenancy: Per-User OAuth & Collectors) — COMPLETE
**Current Focus:** Remaining open issues
**Next Decision Point:** Sprint 28 kickoff or ongoing issue completion
