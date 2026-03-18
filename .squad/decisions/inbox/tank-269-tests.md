# Tank: Decisions for Issue #269 Test Suite — Scriban Template Rendering

## Date
2026-03-17

## Branch
`issue-269` — commit `f98295d`

---

## Files Created

| File | Tests |
|------|-------|
| `src/JosephGuadagno.Broadcasting.Data.Sql.Tests/MessageTemplateDataStoreTests.cs` | 7 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Scriban/ScribanTemplateRenderingTests.cs` | 10 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Twitter/ProcessScheduledItemFiredTests.cs` | 5 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Facebook/ProcessScheduledItemFiredTests.cs` | 5 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/LinkedIn/ProcessScheduledItemFiredTests.cs` | 5 |
| `src/JosephGuadagno.Broadcasting.Functions.Tests/Bluesky/ProcessScheduledItemFiredTests.cs` | 5 |

**Total new tests: 37**  
**All 37 pass. Pre-existing tests unaffected (40/40 Functions.Tests, 126/126 Data.Sql.Tests).**

---

## Decisions

### 1. `MessageTemplateDataStoreTests` placed in `Data.Sql.Tests`
The `MessageTemplateDataStore` is a concrete EF-backed repository in `Data.Sql`. The `Data.Sql.Tests` project already has EF InMemory, AutoMapper with `BroadcastingProfile`, and the xUnit patterns needed. Tests use the InMemory database to verify `GetAsync` (found/not-found/wrong platform/wrong message type/multiple platforms) and `GetAllAsync`.

The `MessageTemplate` entity has a composite primary key `(Platform, MessageType)` — EF InMemory handles this correctly.

### 2. `TryRenderTemplateAsync` is private — tested indirectly via `RunAsync`
All four platform functions expose `TryRenderTemplateAsync` only as `private`. Rather than using reflection (an anti-pattern), the per-platform tests go through the public `RunAsync` API with fully mocked dependencies. This validates the full integration of the template lookup → rendering → fallback logic.

The `EventGridEvent` is constructed with `BinaryData.FromString(json)` where `json` is a serialized `ScheduledItemFiredEvent`. This avoids any real Azure service dependency.

### 3. `ScribanTemplateRenderingTests` — isolated rendering proof
A separate class directly exercises the exact `Template.Parse → ScriptObject.Import → TemplateContext → RenderAsync` pattern that all 4 functions share. This provides:
- Definitive proof that `title`, `url`, `description`, `tags`, `image_url` are all accessible in templates
- Edge-case coverage: null image_url renders as empty string, whitespace-only output returns null, trimming is applied

These tests are platform-agnostic since all 4 functions use identical rendering code.

### 4. `NullLogger<T>.Instance` used instead of `Mock<ILogger<T>>`
All 4 functions make extensive `LogDebug`/`LogInformation`/`LogWarning`/`LogError` and `LogCustomEvent` calls. Using `NullLogger<T>` is simpler and cleaner than configuring `Mock<ILogger<T>>` for extension methods. Tests don't assert on log output — only on return values and mock invocations.

### 5. `SyndicationFeedSources` used as item type in all per-platform tests
The Scriban rendering logic is symmetric across all 4 item types (Feed, YouTube, Engagement, Talk) in each function. Using `SyndicationFeedSources` for all tests keeps the fixture code concise without losing coverage of the fallback/template decision branch. The `ScribanTemplateRenderingTests` covers field-level rendering independently of item type.

### 6. `Functions.Tests` csproj has no `ImplicitUsings`
Unlike `Data.Sql.Tests`, the `Functions.Tests` project does not enable implicit usings. All new test files include explicit `using System;`, `using System.Threading.Tasks;` etc. to match the project convention seen in `LoadNewPostsTests.cs`.

---

## Test Coverage Summary

| Coverage area | Tests | Notes |
|---|---|---|
| `MessageTemplateDataStore.GetAsync` (found) | 2 | Exact match + multi-platform selection |
| `MessageTemplateDataStore.GetAsync` (not found) | 3 | Empty DB, wrong platform, wrong type |
| `MessageTemplateDataStore.GetAllAsync` | 2 | Multiple + empty |
| Scriban field rendering (title, url, description, tags, image_url) | 10 | Isolated; all 5 fields tested individually and together |
| Template found → rendered text used (per platform) | 4 | Twitter, Facebook, LinkedIn, Bluesky |
| Template null → fallback (per platform) | 4 | Twitter/Bluesky → auto-generated; LinkedIn → scheduledItem.Message |
| `image_url` in context when set (per platform) | 4 | Verified in rendered output |
| `image_url` empty when null (per platform) | 4 | Scriban renders null as "" |
| Facebook: `LinkUri` always from item, not template | 1 | Template overrides StatusText only |
| LinkedIn: credentials always from settings | 1 | AuthorId + AccessToken unaffected by template |
| Empty template string → fallback (Twitter, Bluesky) | 2 | Whitespace template → null → fallback |

---

## Gaps / Future Testing Notes

- **`YouTubeSources`, `Engagements`, `Talks` item types** not exercised in per-platform `RunAsync` tests. The Scriban rendering path is the same for all types, but the item-manager mock setup differs. Future tests could add coverage for those branches.
- **`MessageTemplateDataStore.GetAllAsync` sorting/filtering** — no filtering tests since the method returns all rows. If filtering is added later, tests will need updating.
- **Scriban template errors** — the `catch → return null` guard in `TryRenderTemplateAsync` is covered indirectly by the isolated `ScribanTemplateRenderingTests` edge cases, but is not explicitly tested through `RunAsync` (would require mocking template content that causes Scriban to throw).
- **Integration tests** — full end-to-end (Functions.IntegrationTests) would require Aspire AppHost and real DB. Not attempted here.
