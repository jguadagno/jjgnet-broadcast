## 🚀 Production Deployment Runbook — Epic #667 Database Migration

**Prepared by:** Neo (Lead)  
**Date:** 2026-04-08  
**Migration Script:** `scripts/database/migrations/2026-04-08-social-media-platforms.sql`

---

### ⚠️ CRITICAL: This Migration Contains BREAKING CHANGES

**Risk Level:** **HIGH** — Includes composite PK rebuild and column drops  
**Downtime Required:** **YES** — 5-10 minute maintenance window  
**Rollback Complexity:** **HIGH** — Manual SQL required

---

## Pre-Migration Checklist

**All code deployments MUST be complete before running the database migration script.**

### Required Code Changes (Must Land First)

| Issue | Component | Required Changes | Status |
|-------|-----------|------------------|--------|
| Morpheus #667 | Data Layer | SocialMediaPlatforms tables, EF entities, repository | ✅ On branch `issue-667-social-media-platforms` |
| Trinity TBD | Api Layer | Update MessageTemplates endpoints to use `int SocialMediaPlatformId` instead of `string Platform` | ❌ Not started |
| Trinity TBD | Api Layer | Create SocialMediaPlatforms CRUD endpoints | ❌ Not started |
| Cypher TBD | Functions | Update all 4 `ProcessScheduledItemFired` handlers (Twitter, LinkedIn, Facebook, Bluesky) to use `int` param in `GetAsync()` | ❌ Not started |
| Switch TBD | Web Controllers | Update Engagement controllers for social platform collection | ❌ Not started |
| Switch TBD | Web Services | Update `MessageTemplateService` to use `int SocialMediaPlatformId` | ❌ Not started |

### Pre-Flight Verification

- [ ] **All PRs merged to main:** Morpheus, Trinity, Cypher, Switch PRs merged
- [ ] **Build passes on main:** `dotnet build` completes with 0 errors
- [ ] **Tests pass on main:** `dotnet test` completes successfully
- [ ] **All deployments complete:**
  - [ ] Api deployed to Azure App Service `api-jjgnet-broadcast`
  - [ ] Web deployed to Azure App Service `web-jjgnet-broadcast`
  - [ ] Functions deployed to Azure Functions `jjgnet-broadcast`
- [ ] **Deployment health checks pass:** All services responding to health endpoints
- [ ] **Database backup created:** Full backup of `JJGNet` database within last 24 hours
- [ ] **Maintenance window scheduled:** 5-10 minute window communicated to users

---

## Migration Window Requirements

### Recommended Maintenance Window

**Duration:** 10 minutes (includes buffer)  
**Best Time:** Low-traffic period (e.g., Sunday 2:00 AM PT)

### Services to Stop During Migration

**CRITICAL:** The following services MUST be stopped during PART 5 (MessageTemplates PK rebuild):

1. **Azure Functions `jjgnet-broadcast`:** Stop via Azure Portal
   - All 4 `ProcessScheduledItemFired` handlers query MessageTemplates
   - PK rebuild will lock the table
2. **Azure App Service `api-jjgnet-broadcast`:** Optional stop (recommended)
   - Api has MessageTemplates endpoints
3. **Azure App Service `web-jjgnet-broadcast`:** Optional stop (recommended)
   - Web has MessageTemplateService that queries MessageTemplates

**Why:** The MessageTemplates PK rebuild (DROP PK → ADD PK) will cause schema locks. Active queries will fail or timeout.

---

## Step-by-Step Deployment Sequence

### Phase 1: Pre-Deployment (T-30 minutes)

1. **Verify all PRs merged**
   `ash
   git fetch origin
   git log --oneline origin/main -10
   # Confirm Morpheus, Trinity, Cypher, Switch commits present
   `

2. **Verify deployments complete**
   - Check GitHub Actions workflows for Api, Web, Functions (all green)
   - Check Azure Portal: all 3 services show "Running" status
   - Test health endpoints:
     `ash
     curl https://api-jjgnet-broadcast.azurewebsites.net/health
     curl https://web-jjgnet-broadcast.azurewebsites.net/health
     `

3. **Create database backup**
   - Azure Portal → SQL Database `JJGNet` → Backups → "Restore" (verify point-in-time available)
   - OR manual backup: `BACKUP DATABASE JJGNet TO DISK = '...'`

### Phase 2: Maintenance Window Begins (T-0)

4. **Announce maintenance**
   - Post notice to users (if applicable)
   - Update status page (if applicable)

5. **Stop Azure Functions**
   `ash
   az functionapp stop --name jjgnet-broadcast --resource-group <rg-name>
   `
   - Verify in Azure Portal: Status = "Stopped"

6. **Stop Api and Web (recommended)**
   `ash
   az webapp stop --name api-jjgnet-broadcast --resource-group <rg-name>
   az webapp stop --name web-jjgnet-broadcast --resource-group <rg-name>
   `

### Phase 3: Run Migration Script (T+1 minute)

7. **Connect to SQL Server**
   - Use Azure Data Studio or SSMS
   - Connect to Azure SQL Server hosting `JJGNet` database
   - Verify connection: `SELECT DB_NAME()` returns `JJGNet`

8. **Execute migration script**
   `sql
   -- File: scripts/database/migrations/2026-04-08-social-media-platforms.sql
   -- Execute entire script in one transaction
   `

9. **Verify migration success**
   - Check final verification query output:
     `
     SocialMediaPlatforms: 5 rows
     EngagementSocialMediaPlatforms: 0 rows (initially)
     MessageTemplates: <existing count>
     ScheduledItems: <existing count>
     `
   - Verify 5 platforms seeded:
     `sql
     SELECT * FROM dbo.SocialMediaPlatforms ORDER BY Name
     -- Should return: BlueSky, Facebook, LinkedIn, Mastodon, Twitter
     `

10. **Spot-check migrated data**
    `sql
    -- Verify ScheduledItems Platform column dropped
    SELECT TOP 5 SocialMediaPlatformId, MessageType FROM dbo.ScheduledItems

    -- Verify MessageTemplates Platform column dropped, new PK works
    SELECT * FROM dbo.MessageTemplates

    -- Verify Engagements social columns dropped
    SELECT TOP 5 Id, Name FROM dbo.Engagements
    -- Should NOT have BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle
    `

### Phase 4: Restart Services (T+5 minutes)

11. **Restart Azure Functions**
    `ash
    az functionapp start --name jjgnet-broadcast --resource-group <rg-name>
    `

12. **Restart Api and Web**
    `ash
    az webapp start --name api-jjgnet-broadcast --resource-group <rg-name>
    az webapp start --name web-jjgnet-broadcast --resource-group <rg-name>
    `

13. **Verify services healthy**
    `ash
    curl https://api-jjgnet-broadcast.azurewebsites.net/health
    curl https://web-jjgnet-broadcast.azurewebsites.net/health
    `
    - Both should return `200 OK` with `Healthy` status

### Phase 5: Smoke Testing (T+7 minutes)

14. **Test new SocialMediaPlatforms API** (Trinity's work)
    `ash
    # Get all platforms
    curl https://api-jjgnet-broadcast.azurewebsites.net/api/social-media-platforms

    # Should return 5 platforms: Twitter, BlueSky, LinkedIn, Facebook, Mastodon
    `

15. **Test MessageTemplates API** (Trinity's work)
    `ash
    # Get all templates (verify no errors after Platform → SocialMediaPlatformId migration)
    curl https://api-jjgnet-broadcast.azurewebsites.net/api/message-templates
    `

16. **Test Functions** (Cypher's work)
    - Verify no errors in Application Insights for `ProcessScheduledItemFired` handlers
    - Check Azure Portal → Functions → Monitor for recent invocations

17. **Test Web UI** (Switch's work)
    - Navigate to Engagements page
    - Verify no errors, old social fields not visible

### Phase 6: Maintenance Complete (T+10 minutes)

18. **Announce maintenance complete**
    - Remove maintenance notice
    - Update status page

19. **Monitor for 30 minutes**
    - Watch Application Insights for exceptions
    - Check Azure Portal service metrics (CPU, memory, requests)
    - Verify no MessageTemplate-related errors

---

## Rollback Plan

**If migration fails during execution:**

### Immediate Rollback (Before Completing Migration)

If script fails mid-execution (e.g., during PART 5 PK rebuild):

1. **DO NOT restart services**
2. **Restore database from backup**
   - Azure Portal → SQL Database → Restore → select pre-migration restore point
   - OR restore from manual backup
3. **Revert code deployments** (if not rolled back automatically):
   `ash
   # Redeploy previous version from last known good commit
   git log --oneline origin/main -20  # Find commit BEFORE Epic #667 merge
   # Trigger deployments from that commit
   `
4. **Restart services with old code + old schema**

### Delayed Rollback (After Migration Complete, Issues Found)

If migration completes but runtime issues discovered:

1. **Stop services** (Functions, Api, Web)
2. **Restore database** to pre-migration state
3. **Redeploy old code** (commit before Epic #667)
4. **Restart services**

**Data loss risk:** Any ScheduledItems or MessageTemplates created AFTER migration will be lost.

---

## Safe vs. Breaking Changes

### ✅ Safe to Run Now (Additive Only)

If you want to deploy incrementally, the following parts of the migration script are **non-breaking** and can run before code deployment:

- **PART 1:** Create SocialMediaPlatforms table
- **PART 2:** Create EngagementSocialMediaPlatforms junction table
- **PART 3:** Seed SocialMediaPlatforms data

**These parts do NOT break existing code** because they are purely additive (new tables).

### ❌ Breaking — Requires Full Code Deployment First

**DO NOT RUN** until all code changes (Trinity, Cypher, Switch) are deployed:

- **PART 4:** Migrate ScheduledItems.Platform → SocialMediaPlatformId (column drop)
- **PART 5:** Migrate MessageTemplates.Platform → SocialMediaPlatformId (PK rebuild)
- **PART 6:** Remove old social columns from Engagements
- **PART 7:** Remove BlueSkyHandle from Talks

**Why:** These parts drop columns and change composite PK — existing code will crash on column-not-found errors.

---

## Risk Mitigation

### High-Risk Operations

1. **MessageTemplates PK rebuild (PART 5)**
   - **Risk:** Table lock during DROP PK + ADD PK
   - **Mitigation:** Stop all services during this phase (2-3 minutes)
   - **Fallback:** If timeout/deadlock, rollback entire migration

2. **ScheduledItems best-effort Platform mapping (PART 4)**
   - **Risk:** Unknown Platform values (not Twitter/LinkedIn/etc.) will map to NULL
   - **Mitigation:** Script handles NULLs gracefully (FK allows NULL)
   - **Post-migration:** Review ScheduledItems with NULL SocialMediaPlatformId, manually fix

3. **Column drops (PART 6, PART 7)**
   - **Risk:** If old code still deployed, crashes on column access
   - **Mitigation:** Enforce pre-migration checklist — code MUST deploy first

### Monitoring Post-Deployment

**Watch for these errors in Application Insights:**

- `Invalid column name 'Platform'` — Code not updated
- `Cannot insert NULL into SocialMediaPlatformId` — Data mapping issue
- `The INSERT statement conflicted with the FOREIGN KEY constraint` — FK validation issue
- `Deadlock` on MessageTemplates table — Race condition during PK rebuild

**If any occur:** Initiate rollback immediately.

---

## Post-Migration Cleanup

**After 7 days of stable operation:**

1. **Review NULL SocialMediaPlatformId rows**
   `sql
   SELECT * FROM dbo.ScheduledItems WHERE SocialMediaPlatformId IS NULL
   `
   - Manually map to correct platform or delete if orphaned

2. **Close superseded issues**
   - #537, #536, #54, #53 (all replaced by Epic #667)

3. **Update documentation**
   - Mark Epic #667 as complete
   - Update API docs with new SocialMediaPlatforms endpoints

---

## Key Contacts

- **Database Issues:** Morpheus (Data Engineer)
- **API Issues:** Trinity (API Engineer)
- **Functions Issues:** Cypher (Functions Engineer)
- **Web Issues:** Switch (Web Engineer)
- **Deployment Coordinator:** Neo (Lead)
- **Final Approval:** Joseph Guadagno (Product Owner)

---

## Summary

| Phase | Duration | Downtime | Risk |
|-------|----------|----------|------|
| Pre-Deployment | 30 min | No | Low |
| Stop Services | 1 min | **YES** | Low |
| Run Migration | 3 min | **YES** | **HIGH** |
| Restart Services | 1 min | **YES** | Low |
| Smoke Testing | 3 min | No | Low |
| **Total** | **38 min** | **5 min** | **HIGH** |

**Bottom Line:** Do not run the database migration script until ALL code changes (Morpheus, Trinity, Cypher, Switch) are deployed to production. The MessageTemplates PK rebuild requires a brief maintenance window with all services stopped.

