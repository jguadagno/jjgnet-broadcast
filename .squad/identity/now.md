# Team Focus — Now

> **Last updated:** 2026-04-24T21:30:57Z
> **Sprint:** 27 (in progress) — Milestone 21
> **Status:** Core multi-tenancy work merged. Joe validating site functionality; #855 and #856 will close after validation. #852 and #853 remain open — deferred to next sprint.

## Current Focus

**Sprint 27 — IN PROGRESS** 🔄

**Completed:**
- ✅ #777 — Per-user OAuth/token runtime (merged via PR #854)
- ✅ #778 — Per-user collector onboarding/configuration (merged via PR #859)
- ✅ #858 — Manual DB migration for per-user collector config tables (Joe, production 2026-04-24)

**Pending (Joe validating):**
- ⏳ #855 — Closes after site usability/config validation passes
- ⏳ #856 — Closes after site usability/config validation passes

**Deferred to next sprint:**
- 🔜 #852 — Next sprint
- 🔜 #853 — Next sprint

**Goal:** Complete #855 and #856 after Joe's validation. Then begin next sprint with #852 and #853.

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
