# Decision: No Raw SQL in ScheduledItemDataStore

**Date:** 2026-03-16
**Source:** PR #280 review comment by @jguadagno
**Applies to:** Morpheus, Trinity (any data store work)

## Rule

Do NOT use FromSqlRaw, ExecuteSqlRaw, or any hardcoded SQL strings in ScheduledItemDataStore (or any DataStore).
Use **Entity Framework Core LINQ queries** instead. When type-based dispatch is needed (e.g. per ScheduledItemType), use the enum directly in LINQ predicates.

## Example (correct approach for orphan detection)

Use EF DbSets with .Where() and .ContainsAsync() / HashSet membership — do not write raw SQL.
