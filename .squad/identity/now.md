# Team Focus — Now

> **Last updated:** 2026-05-01T12:00:00Z
> **Sprint:** 30 — Milestone 26
> **Status:** Sprint 29 CLOSED (PR #909 #890/#893 + PR #910 misc fixes merged to main). Sprint 30 ACTIVE. #897 (ISocialMediaPublisher interface) COMPLETE. #902→#899–#900–#901 composition refactor sequence UNBLOCKED.
> **Next:** #902 (LinkedIn composition) starts immediately; #899–#900–#901 (Twitter, Facebook, Bluesky) follow after #902 pattern validation. Joe executes parallel tasks (#892, #856, #896) independently.

## Current Focus

**Sprint 28 — COMPLETE** ✅
- ✅ #852 — LinkedIn OAuth token expiry data layer (merged)
- ✅ #853 — Token expiry notification API (merged)
- ✅ #904 — Code quality session (merged PR #908)

**Sprint 29 — COMPLETE** ✅
- ✅ #890 — Add from ≤ to guard to GetExpiringWindowAsync (Trinity implementation + Tank tests)
- ✅ #893 — Log warning when Settings:WebBaseUrl is missing (Trinity implementation + Tank tests)
- ✅ PR #909 merged (#890 + #893 implementation)
- ✅ PR #910 merged (misc fixes)

**Sprint 30 — ACTIVE** 🔄
- ✅ #897 — Define ISocialMediaPublisher common interface (COMPLETE — test coverage finalized, DI wiring verified)
- 🔜 #902 — Move LinkedIn message composition to LinkedInManager (start immediately after #897 merge)
- 🔜 #899, #900, #901 — Move Twitter/Facebook/Bluesky message composition (parallel after #902 pattern validation)

**Joe's Parallel Tasks (Non-blocking):**
- ⏳ #892 — Add Settings:WebBaseUrl config to Functions and Web (post-#893)
- ⏳ #856 — Retire LinkedIn Key Vault secrets in Azure Portal
- ⏳ #896 — Move jjgnet resource group to new Azure subscription

**Goal:** Sprint 30 unblocks #897 (ISocialMediaPublisher interface) COMPLETE. Composition refactor sequence (#902→#899–#900–#901) begins immediately. Joe executes parallel infrastructure tasks independently.

## Key Patterns (Sprint 30)

1. **Shared publisher interface:** `ISocialMediaPublisher` with platform identity + unified `PublishAsync(SocialMediaPublishRequest)` entry point
2. **Three-layer test strategy:** Interface shape + inheritance validation + platform-specific routing proof (avoids test duplication)
3. **Composition refactor sequence:** #902 (LinkedIn pattern reference) → #899–#900–#901 (Twitter/Facebook/Bluesky parallel) after pattern validation
4. **Risk mitigation:** Serial pattern (#902 first) reduces merge conflicts from ISocialMediaPublisher implementation divergence
5. **Backward compatibility:** Existing manager interfaces + queue handlers unchanged; shared contract enables future runtime discovery

## Standing Work

- #897–#902: ISocialMediaPublisher interface + per-platform composition refactor (Sprint 30, #897 COMPLETE)
- #902: LinkedIn composition refactor (Tank team, next assignment post-#897 merge)
- #899–#900–#901: Twitter, Facebook, Bluesky composition refactors (Tank team, parallel after #902 validation)
- #892: Settings:WebBaseUrl config (Joe, post-#893)
- #856, #896: Joe's Azure Portal + infrastructure tasks (non-blocking, async)
- #724: Multi-user teams/groups — Deferred pending explicit Joe confirmation that #609 is 100% production-ready
- #803: Allow `.squad` updates to commit from main (squad:neo, low priority)

## Team Composition

**Sprint 30 (Active):**
- #897 (ISocialMediaPublisher) — ✅ COMPLETE
- #902 (LinkedIn composition) — Next assignment pending
- Joe — Parallel tasks (#892, #856, #896) — can execute independently

**Rotating Roles:** Squad roster available in `.squad/team.md`

---

**Last Updated:** 2026-05-01T12:00:00Z
**Sprint:** 30 (ISocialMediaPublisher + Composition Refactor) — #897 COMPLETE
**Current Focus:** #902 (LinkedIn composition refactor) — begins immediately post-#897 merge; #899–#900–#901 ready for parallel execution after #902 pattern validation
**Next Decision Point:** #902 completion unlocks #899–#900–#901 parallel execution
