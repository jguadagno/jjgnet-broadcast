# Decision: Epic #667 Database Layer Review — Breaking Change Deployment Strategy

**Date:** 2026-04-08  
**Author:** Neo (Lead)  
**Issue:** #667 — Move social links for engagements into its own table  
**Branch:** `issue-667-social-media-platforms`  
**Commit:** 3fc341e

---

## Context

Reviewed Morpheus's database layer implementation for Epic #667 on local branch `issue-667-social-media-platforms`. The work is architecturally sound and complete, but introduces a breaking change to `IMessageTemplateDataStore.GetAsync()` signature that affects 3 projects (Api, Web, Functions) and causes 14 compile errors.

The PR does not exist yet — branch has not been pushed to GitHub.

---

## Review Findings

### ✅ Passes All Architecture Requirements

**Database schema:**
- SocialMediaPlatforms: Id (PK), Name (unique), Url, Icon (Bootstrap class), IsActive (soft delete)
- EngagementSocialMediaPlatforms: Composite PK (EngagementId, SocialMediaPlatformId), Handle field
- ScheduledItems: Platform (nvarchar) dropped, SocialMediaPlatformId (int FK) added
- MessageTemplates: Platform (string, part of composite PK) migrated to SocialMediaPlatformId (int FK) with PK rebuild
- Engagements: 3 social columns dropped (BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle)
- Talks: BlueSkyHandle dropped

**Migration script quality:**
- 7-part structured migration (`scripts/database/migrations/2026-04-08-social-media-platforms.sql`)
- Correct PK rebuild sequence: add column → populate → drop old PK → create new PK → drop old column
- Best-effort string mapping for ScheduledItems.Platform → SocialMediaPlatformId
- Seed data: 5 platforms (Twitter, BlueSky, LinkedIn, Facebook, Mastodon)

**Code layer coverage:**
- EF Core entities match SQL schema
- Domain models have proper nullable annotations
- ISocialMediaPlatformDataStore interface with CRUD + soft delete
- SocialMediaPlatformDataStore repository implementation
- AutoMapper profiles (bidirectional)
- DI registration in Api Program.cs

### ❌ Build Errors (Expected Breaking Change)

**14 compile errors across:**
- `Data.Sql.Tests/MessageTemplateDataStoreTests.cs` (6 errors)
- `Api/MappingProfiles/ApiBroadcastingProfile.cs` (1 error)
- `Api/Controllers/MessageTemplatesController.cs` (2 errors)
- `Web/Services/MessageTemplateService.cs` (1 error)
- `Functions/*/ProcessScheduledItemFired.cs` (4 errors — LinkedIn, Bluesky, Facebook, Twitter)

**Root cause:** `IMessageTemplateDataStore.GetAsync` signature changed from:
- **Old:** `GetAsync(string platform, string messageType)`
- **New:** `GetAsync(int socialMediaPlatformId, string messageType)`

This was documented in commit message as expected breaking change requiring Trinity and Cypher follow-up work.

---

## Decision: Deployment Strategy for Breaking DB Migrations

**Pattern established for breaking database migrations involving PK rebuilds or column drops:**

### 1. Code Deploys First (Always)

**All code changes MUST be deployed to production before running database migration script.**

**Enforcement:**
- Pre-migration checklist in deployment runbook requires all PRs merged and deployed
- Breaking parts of migration script (column drops, PK rebuilds) isolated in separate sections

**For Epic #667 specifically:**
- Morpheus PR (Data layer) + Trinity PRs (Api layer) + Cypher PRs (Functions) + Switch PRs (Web layer)
- All PRs must merge to main
- All 3 Azure deployments (Api, Web, Functions) must complete
- Build must pass with 0 errors on main branch

### 2. Maintenance Window Required for PK Rebuilds

**MessageTemplates composite PK rebuild requires brief downtime:**
- Duration: 5-10 minutes (includes buffer)
- Services stopped: Functions (required), Api (recommended), Web (recommended)
- Reason: DROP PK + ADD PK causes table lock, active queries will fail

**When to use:**
- Composite PK changes
- High-traffic tables with schema locks
- Operations that cannot run under active load

### 3. Incremental Migration Option

**Additive changes can run separately before code deployment:**

**Safe to run now (Epic #667 Parts 1-3):**
- Create SocialMediaPlatforms table
- Create EngagementSocialMediaPlatforms junction table
- Seed SocialMediaPlatforms data

**These do NOT break existing code** — purely additive.

**Breaking changes wait for code deployment (Epic #667 Parts 4-7):**
- Migrate ScheduledItems.Platform → SocialMediaPlatformId (column drop)
- Migrate MessageTemplates.Platform → SocialMediaPlatformId (PK rebuild)
- Drop old social columns from Engagements
- Drop BlueSkyHandle from Talks

**When to use:**
- Migrations with mix of additive and breaking changes
- Allows pre-staging lookup tables and seed data before code deployment
- Reduces risk during maintenance window (additive parts already validated)

### 4. Deployment Runbook Mandatory

**Complex migrations require step-by-step runbook with:**
- Pre-migration checklist (code deployment verification)
- Service stop/start sequence
- Step-by-step SQL execution instructions
- Verification queries
- Rollback plan
- Safe vs. breaking change breakdown
- Key contacts

**Epic #667 runbook posted:** https://github.com/jguadagno/jjgnet-broadcast/issues/667#issuecomment-4210318810

### 5. Rollback Plan

**For database restore rollback:**
- Stop all services
- Restore database from pre-migration backup (Azure point-in-time restore)
- Redeploy previous code version (commit before Epic #667 merge)
- Restart services

**Data loss risk:** Any records created AFTER migration will be lost in rollback.

---

## Recommendation

**Conditional Approval for Epic #667:**

✅ **Database layer work is production-ready**  
✅ **SQL migration script is high quality**  
❌ **Cannot merge until downstream PRs resolve build errors**  
❌ **Cannot deploy until deployment runbook followed**

**Next steps:**
1. **Morpheus:** Push branch `issue-667-social-media-platforms`, create PR with detailed description
2. **Trinity:** Create follow-up PRs to update Api MessageTemplates endpoints and create SocialMediaPlatforms CRUD endpoints
3. **Cypher:** Create follow-up PR to update all 4 Functions `ProcessScheduledItemFired` handlers
4. **Switch:** Create follow-up PRs to update Web MessageTemplateService and Engagement controllers
5. **Neo:** Final review after all PRs created and build passes
6. **Joseph:** Execute deployment runbook during scheduled maintenance window after all PRs merged

---

## Impact

### Pattern Reuse

This breaking-change deployment strategy applies to:
- Any migration with column drops (affects existing queries)
- Any migration with PK/index rebuilds (table locks)
- Any migration changing interface signatures (compile-time breaks)

### Future Migrations

**When planning breaking migrations:**
1. Document breaking changes in commit message and PR description
2. Identify affected projects/files BEFORE merge
3. Coordinate dependent PRs across squads
4. Create deployment runbook with pre-flight checklist
5. Schedule maintenance window if PK rebuild or table lock required

**When reviewing breaking migrations:**
1. Verify all affected code identified
2. Check for maintenance window requirement
3. Validate incremental migration option (additive first, breaking later)
4. Require deployment runbook for complex migrations

---

## Files Reviewed

- `scripts/database/migrations/2026-04-08-social-media-platforms.sql` (279 lines)
- `scripts/database/table-create.sql` (additions)
- `scripts/database/data-seed.sql` (additions)
- 24 C# files (753 insertions, 61 deletions)

**Full review document:** `neo-review-667.md` (local file)  
**Deployment runbook:** Posted to issue #667

---

## References

- Epic #667: https://github.com/jguadagno/jjgnet-broadcast/issues/667
- Deployment runbook: https://github.com/jguadagno/jjgnet-broadcast/issues/667#issuecomment-4210318810
- Branch: `issue-667-social-media-platforms` (local, not pushed)
- Commit: 3fc341e
