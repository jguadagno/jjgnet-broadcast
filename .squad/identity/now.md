# Team Focus — Now

> **Last updated:** 2026-04-28T05:30:00Z
> **Sprint:** 28 — Milestone 22
> **Status:** Sprint 27 complete. #855 closed. #856 flagged to Joe (manual Azure Portal step). Sprint 28 begins with #852 (Trinity, in flight) and #853 (blocked on #852 merge).
> **Next:** Merge #852, unblock #853. Complete Joe's manual #856 step asynchronously.

## Current Focus

**Sprint 28 — IN PROGRESS** 🔄

**Sprint 27 — COMPLETE:**
- ✅ #777 — Per-user OAuth/token runtime (merged via PR #854)
- ✅ #778 — Per-user collector onboarding/configuration (merged via PR #859)
- ✅ #858 — Manual DB migration for per-user collector config tables (Joe, production 2026-04-24)
- ✅ #855 — Site validation complete (closed)

**Sprint 28 — ACTIVE:**
- 🔄 #852 — LinkedIn OAuth token expiry data layer (Trinity, in flight)
  - LastNotifiedAt column addition
  - GetExpiringWindowAsync repository method
  - Domain model + manager + unit tests
- 🔜 #853 — Blocked on #852 merge (Trinity, next after merge)
- ⏳ #856 — Manual production step pending Joe (squad:Joe label)
  - Disable old LinkedIn Key Vault secrets in Azure Portal
  - Non-blocking code task

**Goal:** Merge #852 → unblock #853. Joe completes #856 Azure Portal step asynchronously. Monitor CodeQL CSRF alert #41 for next scan resolution.

## Key Patterns (Sprint 28)

1. **LinkedIn OAuth expiry tracking:** New `LastNotifiedAt` column tracks when users were last notified of token expiry (supports #852 → #853 notification flow).
2. **Per-user OAuth:** Continues from Sprint 27 — `LinkedinController` acquires and stores tokens per user.
3. **Per-user collectors:** `UserCollectors` data model; queries filter by current user's OID — no cross-user leakage.
4. **Security invariant:** Tests prove a user cannot read or execute another user's tokens or collector configurations.

## Standing Work

- #803: Allow `.squad` updates to commit from main (squad:neo, low priority)
- #724: Multi-user teams/groups — Epic #609 now complete, ready for implementation
- CodeQL CSRF alert #41 — monitoring, may resolve on next scan

## Team Composition

**Sprint 28 (Active):**
- Trinity — Implementation (#852, #853) 🔄
- Joe — Manual Azure Portal step (#856) ⏳

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-04-28T05:30:00Z
**Sprint:** 28 (LinkedIn OAuth Token Expiry) — IN PROGRESS
**Current Focus:** #852 (Trinity), #853 (blocked), #856 (Joe manual step)
**Next Decision Point:** #852 merge completion
