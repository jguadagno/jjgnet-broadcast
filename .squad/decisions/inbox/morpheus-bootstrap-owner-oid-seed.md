# Decision: bootstrap owner OID seed for issue #760

- **Date:** 2026-04-20
- **Owner:** Morpheus
- **Context:** PR #771 resolves collector ownership by reading the
  newest persisted source record owner OID and failing closed when no
  owner can be found. On a fresh database, the base seed script created
  source rows but did not assign any owner OID values, leaving the new
  resolver without a bootstrap path.

## Decision

Use one seed-script variable near the top of
`scripts/database/data-seed.sql` as the single source of truth for
seeded ownership:

```sql
DECLARE @SeededOwnerEntraOid nvarchar(36) = N'00000000-0000-0000-0000-000000000000';
```

Add a TODO comment directly above it telling operators to replace the
placeholder with a real Entra object ID when they want seeded ownership
to map to a real user. Reuse that variable everywhere the bootstrap
seed creates owner-aware records.

## Why

1. **Fresh-database bootstrap must succeed.**
   `SyndicationFeedSources` and `YouTubeSources` now require a usable
   owner path for fail-closed collector resolution.
2. **One replacement point beats scattered literals.**
   Operators can update one obvious value instead of hunting through
   hundreds of seed rows.
3. **Scope stays narrow.**
   This fixes the clean-environment gap without changing resolver
   behavior, schema-loading order, or introducing migrations.

## Consequences

- Fresh environments get deterministic seeded ownership immediately.
- Operators still need to replace the placeholder GUID with a real
  Entra object ID when they want seeded records to belong to a real
  user.
- Future seed additions to owner-aware tables should reuse
  `@SeededOwnerEntraOid` instead of embedding a new literal.
