---
name: "collector-owner-oid-resolution"
description: "Resolve collector ownership from persisted source records and fail closed on missing owner OIDs."
domain: "azure-functions"
confidence: "high"
source: "earned"
tools:
  - name: "rg"
    description: "Find collector call sites, reader overloads, and owner field usage."
    when: "When tracing ownership flow across Functions, readers, managers, data stores, and tests."
---

## Context

Use this when a background collector has no authenticated user context but still needs to persist owned records. In JJGNet Broadcasting, the Round 1 fix is to resolve the owner OID from existing persisted source records and pass that OID all the way into the reader that materializes new content.

## Patterns

- Add a narrow `GetCollectorOwnerOidAsync` helper on the relevant manager/data-store pair.
- Resolve the owner OID at the Function boundary before calling the reader.
- If the owner OID is missing or blank, fail closed and stop before persistence.
- Reader methods that materialize persistable domain models should require `ownerOid` and throw on blank values.
- Regression coverage should stub `GetCollectorOwnerOidAsync(...)` with a distinct owner value and verify the collector passes that exact value into `reader.GetAsync(...)`.
- Regression coverage should verify `SaveAsync(...)` receives records whose `CreatedByEntraOid` is non-empty and matches the resolved owner.
- Keep manually skipped integration tests compiling against the owner-aware overloads so repo-wide builds stay green.
- Keep the scope narrow: do not broaden a collector-owner fix into OAuth/token-runtime redesign unless the issue explicitly asks for it.

## Examples

- `src\JosephGuadagno.Broadcasting.Functions/Collectors/CollectorOwnerOidResolver.cs`
- `src\JosephGuadagno.Broadcasting.Functions/Collectors/SyndicationFeed/LoadNewPosts.cs`
- `src\JosephGuadagno.Broadcasting.Functions/Collectors/YouTube/LoadNewVideos.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests/Collectors/LoadNewPostsTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests/Collectors/LoadAllVideosTests.cs`
- `src\JosephGuadagno.Broadcasting.SyndicationFeedReader.Tests/SyndicationFeedReaderOfflineTests.cs`
- `src\JosephGuadagno.Broadcasting.YouTubeReader.Tests/YouTubeReaderFetchTests.cs`

## Anti-Patterns

- Do not fall back to a global settings-level owner scaffold once source records already carry ownership.
- Do not allow readers to construct persistable records with `CreatedByEntraOid = string.Empty`.
- Do not hide missing-owner conditions by silently saving data with blank ownership.
- Do not let skipped/manual integration tests drift onto removed overloads after the owner-aware interfaces become the contract.
