## Neo's Review: Epic #667 Database Layer (Branch: issue-667-social-media-platforms)

**Status:** ❌ **CANNOT APPROVE - PR DOES NOT EXIST YET**  
**Reviewed Commit:** 3fc341e  
**Reviewer:** Neo (Lead)  
**Date:** 2026-04-08

---

### Executive Summary

Morpheus has completed the database layer work for Epic #667 on branch `issue-667-social-media-platforms` (commit 3fc341e), but **has not yet pushed the branch or created a PR**. The work is comprehensive and architecturally sound, but **introduces 14 compile errors** in Api, Web, and Functions projects — this is **expected and documented** as a breaking change requiring Trinity's and Cypher's follow-up work.

The local branch is ready for PR creation after build errors are resolved.

---

### Review Against Architecture Decisions

#### ✅ **PASS: Database Schema**
- **SocialMediaPlatforms table:** Id (PK identity), Name (unique), Url, Icon (Bootstrap class), IsActive (soft delete) — **correct**
- **EngagementSocialMediaPlatforms junction:** EngagementId FK, SocialMediaPlatformId FK, Handle (nvarchar 200), composite PK — **correct**
- **Talks:** No separate junction table, inherit from parent Engagement — **correct** (removed BlueSkyHandle column)
- **ScheduledItems.Platform:** Dropped, replaced with SocialMediaPlatformId int FK — **correct**
- **MessageTemplates.Platform:** Migrated to SocialMediaPlatformId with PK rebuild — **correct** (complex migration handled properly)
- **Soft delete:** IsActive bool — **correct**
- **Seed data:** Twitter, BlueSky, LinkedIn, Facebook, Mastodon — **correct**

#### ✅ **PASS: SQL Migration Script Quality**
File: `scripts/database/migrations/2026-04-08-social-media-platforms.sql`

**Strengths:**
- Well-structured 7-part migration with clear comments
- Correct order: create tables → seed → migrate ScheduledItems → migrate MessageTemplates (PK rebuild) → drop old columns
- Best-effort string mapping for ScheduledItems.Platform (Twitter/twitter/X/x → Twitter ID, etc.)
- Proper PK drop/recreate sequence for MessageTemplates (nullable → populate → NOT NULL → new PK → FK)
- Verification queries at end

**Risks properly handled:**
- ScheduledItems.Platform nullable during migration (safe)
- MessageTemplates PK rebuild while zero traffic (documented in runbook requirement)

#### ✅ **PASS: Base Scripts Updated**
- `table-create.sql`: Reflects post-migration schema (no old columns, includes new tables + FKs)
- `data-seed.sql`: Includes 5 platform seed INSERTs with `IF NOT EXISTS` guards

#### ✅ **PASS: EF Core Entities Match SQL**
All entity models in `Data.Sql/Models/` correctly reflect schema:
- `SocialMediaPlatform.cs`: Id, Name, Url, Icon, IsActive, EngagementSocialMediaPlatforms nav property
- `EngagementSocialMediaPlatform.cs`: Composite key, Handle, both nav properties
- `Engagement.cs`: Removed 3 social fields, added `SocialMediaPlatforms` collection
- `Talk.cs`: Removed BlueSkyHandle
- `ScheduledItem.cs`: SocialMediaPlatformId int?, SocialMediaPlatform nav property
- `MessageTemplate.cs`: SocialMediaPlatformId int (not string), SocialMediaPlatform nav property

#### ✅ **PASS: BroadcastingContext Configuration**
`BroadcastingContext.cs` correctly configures:
- Composite PKs on both new tables
- Unique index on SocialMediaPlatforms.Name
- FK relationships with constraint names matching SQL
- MaxLength on all string fields
- IsActive default value

#### ✅ **PASS: Domain Models - Nullable Annotations**
All domain models in `Domain/Models/` have proper nullability:
- `SocialMediaPlatform`: Required on Id, Name; nullable on Url, Icon
- `EngagementSocialMediaPlatform`: Required on both FKs, nullable Handle
- `Engagement.SocialMediaPlatforms`: Nullable collection
- `MessageTemplate.SocialMediaPlatformId`: Required (`[Required]` attribute)
- `ScheduledItem.SocialMediaPlatformId`: Nullable `int?`

#### ✅ **PASS: ISocialMediaPlatformDataStore Interface**
`Domain/Interfaces/ISocialMediaPlatformDataStore.cs` covers:
- GetAsync(int id)
- GetAllAsync(bool includeInactive = false)
- AddAsync
- UpdateAsync
- DeleteAsync (soft delete) — **correct**

#### ✅ **PASS: Repository Implementation**
`SocialMediaPlatformDataStore.cs`:
- Soft delete: sets `IsActive = false` (not hard delete) — **correct**
- GetAllAsync filters by IsActive unless includeInactive flag set — **correct**
- Ordered by Name
- Try/catch with null return on failure (matches project pattern)

#### ✅ **PASS: AutoMapper Profiles**
`MappingProfiles/BroadcastingProfile.cs` includes:
- `SocialMediaPlatform` bidirectional mapping (ReverseMap)
- `EngagementSocialMediaPlatform` bidirectional mapping (ReverseMap)

#### ✅ **PASS: DI Registration**
`Api/Program.cs` registers `ISocialMediaPlatformDataStore` → `SocialMediaPlatformDataStore` — **correct**

#### ❌ **FAIL: No DateTime - BLOCKED BY BUILD ERRORS**
Cannot verify DateTimeOffset compliance — **14 compile errors** prevent full codebase scan. Migration script uses `datetimeoffset` in existing table references (correct).

#### ❌ **FAIL: Build Does Not Pass**
**14 compile errors** in:
- `Data.Sql.Tests/MessageTemplateDataStoreTests.cs` (6 errors) — uses `Platform` string, needs `SocialMediaPlatformId` int
- `Api/MappingProfiles/ApiBroadcastingProfile.cs` (1 error) — maps `Platform` field
- `Api/Controllers/MessageTemplatesController.cs` (2 errors) — GetAsync signature + Platform access
- `Web/Services/MessageTemplateService.cs` (1 error) — Platform field access
- `Functions/*/ProcessScheduledItemFired.cs` (4 errors across LinkedIn, Bluesky, Facebook, Twitter) — GetAsync signature

**Root cause:** These files still reference `MessageTemplate.Platform` (string) instead of `SocialMediaPlatformId` (int). This is **expected and documented** in commit message as breaking change requiring Trinity and Cypher's work.

#### ❌ **FAIL: Breaking Change Not Mitigated**
**IMessageTemplateDataStore.GetAsync** signature changed:
- **Old:** `GetAsync(string platform, string messageType)`
- **New:** `GetAsync(int socialMediaPlatformId, string messageType)`

This breaks **4 Azure Functions** (all `ProcessScheduledItemFired` handlers) and Web project. **No interim compatibility layer** was provided.

**Recommended:** This should block PR merge until Trinity provides API layer updates (or Functions are temporarily disabled in deployment runbook).

---

### Review Against Project Conventions

#### ✅ **PASS: Raw SQL Scripts (NOT EF Migrations)**
Migration in `scripts/database/migrations/` as raw SQL — **correct**

#### ⚠️ **PARTIAL: AutoMapper Profiles**
New entities have AutoMapper profiles, but **existing downstream code not updated** (blocked by build errors)

#### ⚠️ **CANNOT VERIFY: Web Project Calls Managers Only**
Build errors prevent verification — Web/Services/MessageTemplateService.cs references MessageTemplate.Platform field

#### ✅ **PASS: Repository Pattern**
`SocialMediaPlatformDataStore` follows established repository pattern (try/catch, SaveChangesAsync, null returns on failure)

---

### Additional Findings

#### ✅ **STRENGTHS:**
1. **Excellent migration script** — thorough, well-commented, correct ordering
2. **Complete layer coverage** — SQL, EF entities, domain models, interface, repository, AutoMapper, DI
3. **Proper PK rebuild strategy** for MessageTemplates (risky operation handled correctly)
4. **Soft delete implemented correctly** with IsActive flag and GetAllAsync filter
5. **Base scripts updated** — future clean installs will use new schema
6. **Commit message** clearly documents breaking changes and next steps

#### ❌ **BLOCKERS:**
1. **Build fails** — 14 compile errors across 3 projects
2. **No PR exists** — branch not pushed to origin
3. **Breaking change affects production services** (Azure Functions) — requires deployment coordination

#### ⚠️ **RISKS:**
1. **MessageTemplates PK migration** requires **zero active traffic** — needs maintenance window
2. **ScheduledItems best-effort string mapping** may produce null SocialMediaPlatformId for unknown Platform values
3. **No rollback tested** — if migration fails mid-flight, manual SQL required

---

### Deployment Coordination Required

This PR **cannot be deployed independently**. See deployment runbook comment on #667.

**Minimum prerequisite PRs before DB migration:**
- Trinity: Update Api MessageTemplates endpoints to use int SocialMediaPlatformId
- Cypher: Update all 4 Functions ProcessScheduledItemFired handlers to use int param
- Switch: Update Web MessageTemplateService to use int SocialMediaPlatformId

**Migration window:** Requires **5-10 minute maintenance window** during MessageTemplates PK rebuild (PART 5 of migration script).

---

### Recommendation

**CONDITIONAL APPROVAL** (pending PR creation):

✅ **Database layer work is architecturally sound and complete**  
✅ **SQL migration script is production-ready**  
❌ **Cannot merge until build errors resolved by Trinity and Cypher**  
❌ **Cannot deploy until deployment runbook followed**

**Next steps:**
1. **Morpheus:** Push branch, create PR with detailed description
2. **Trinity:** Update Api layer (#668, #669 per epic breakdown)
3. **Cypher:** Update Functions layer
4. **Neo:** Review final PR after build passes
5. **Joseph:** Execute deployment runbook during scheduled maintenance window

---

**Reviewed files:**
- scripts/database/migrations/2026-04-08-social-media-platforms.sql (279 lines)
- scripts/database/table-create.sql (additions)
- scripts/database/data-seed.sql (seed additions)
- 24 C# files modified (753 insertions, 61 deletions)

**Compliance:** Follows all architectural decisions from issue #667. Breaking changes documented and expected.

