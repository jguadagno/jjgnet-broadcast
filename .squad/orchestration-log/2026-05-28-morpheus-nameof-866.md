### 2026-05-28 — Morpheus: Sort Property Name Refactor (#866)

| Field | Value |
|-------|-------|
| **Agent routed** | Morpheus (Data Engineer) |
| **Why chosen** | Code review flagged hard-coded sort strings violating compile-time safety directive; Morpheus specializes in data layer consistency |
| **Mode** | sync |
| **Why this mode** | Refactor is self-contained within DataStores; no dependent work blocks progression |
| **Files authorized to read** | 9 DataStore files: EngagementDataStore.cs, MessageTemplateDataStore.cs, ScheduledItemDataStore.cs, SocialMediaPlatformDataStore.cs, SyndicationFeedSourceDataStore.cs, YouTubeSourceDataStore.cs, UserCollectorFeedSourceDataStore.cs, UserCollectorYouTubeChannelDataStore.cs, UserPublisherSettingDataStore.cs |
| **File(s) agent must produce** | Commit 1378c3b — all 9 DataStore files with nameof().ToLowerInvariant() refactored sort-by logic |
| **Outcome** | Completed — 27 hard-coded sort string literals replaced with nameof().ToLowerInvariant() across 9 DataStore files; build clean (0 errors, 0 warnings); committed as 1378c3b |

---

## Summary

During PR #867 code review, Joseph flagged that all DataStore sort-by switch statements used hard-coded string literals for property names, violating the team directive that requires `nameof(Property).ToLowerInvariant()` for compile-time safety.

Morpheus replaced all 27 hard-coded string literals with idiomatic C# if/else chains using `nameof()`, ensuring that property renames are caught by the compiler instead of silently failing at runtime.

## Key Changes

- **All 9 DataStores:** Converted sort-by logic from switch expressions with hard-coded strings to if/else chains using `nameof(EntityType.PropertyName).ToLowerInvariant()`
- **Property transformations maintained:** Preserved existing API contracts via `.Replace()` for properties like `EndDateTime` → `"enddate"`
- **Compile-time safety:** Property renames now trigger compiler errors instead of silent runtime failures
- **Build result:** ✅ Clean (0 errors, 0 warnings)

## Files Changed

All in `src\JosephGuadagno.Broadcasting.Data.Sql\`:

1. EngagementDataStore.cs — 2 overloads
2. MessageTemplateDataStore.cs — 2 overloads
3. ScheduledItemDataStore.cs — 2 overloads
4. SocialMediaPlatformDataStore.cs — 1 overload
5. SyndicationFeedSourceDataStore.cs — 2 overloads
6. YouTubeSourceDataStore.cs — 2 overloads
7. UserCollectorFeedSourceDataStore.cs — 1 overload
8. UserCollectorYouTubeChannelDataStore.cs — 1 overload
9. UserPublisherSettingDataStore.cs — 1 overload

**Total:** 18 paged `GetAllAsync` overloads updated; 27 hard-coded strings replaced

## Related Work

- Issue #866: GetAll Consistency standardization
- PR #867: Code review that identified the violation
- User directive: Always use `nameof().ToLowerInvariant()` for property names in sort-by logic
