# Team Focus — Now

> **Last updated:** 2026-04-24
> **Sprint:** 27 (active) — Milestone 21
> **Status:** Sprint 26 (Milestone 22) closed. Starting Sprint 27 multi-tenancy work.
> **Next:** Implement per-user OAuth runtime (#777) and per-user collector configuration (#778).

## Current Focus

**Sprint 27 — Multi-Tenancy: Per-User OAuth & Collector Configuration (Epic #609)**

| Issue | Title | Squad |
|---|---|---|
| #777 | Per-user OAuth/token runtime: replace shared Key Vault pattern | trinity |
| #778 | Per-user collector onboarding/configuration: complete acceptance criteria | trinity |

**Goal:** Replace the shared Key Vault OAuth pattern with per-user token acquisition/storage, and complete per-user collector configuration so each user manages their own RSS/YouTube sources — no cross-user visibility.

## Key Patterns (Sprint 27)

1. **Per-user OAuth:** `LinkedinController` (and other platforms) must acquire and store tokens per user, not from a shared Key Vault secret.
2. **Per-user collectors:** `UserCollectors` data model; queries filter by current user's OID — no cross-user leakage.
3. **Security invariant:** Tests must prove a user cannot read or execute another user's tokens or collector configurations.

## Standing Work

- #803: Allow `.squad` updates to commit from main (squad:neo, low priority)
- #724: Multi-user teams/groups — blocked on Epic #609 completion

## Team Composition

**Sprint 27:**
- Trinity — Implementation (#777, #778)
- Tank — Test coverage
- Neo — Review & architecture

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-04-24
**Sprint:** 27 (Per-User OAuth & Collectors)
**Next Decision Point:** Sprint 28 kickoff after Sprint 27 closes
