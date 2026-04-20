---
name: "seed-owner-oid-bootstrap"
description: "Use a single seed-script owner OID variable when bootstrap data must satisfy owner-aware logic on fresh databases."
domain: "sql-server"
confidence: "high"
source: "earned"
tools:
  - name: "view"
    description: "Inspect base seed scripts and owner-aware table definitions."
    when: "When checking which seeded records require CreatedByEntraOid or similar owner columns."
  - name: "rg"
    description: "Find owner columns and bootstrap inserts that need to share one seed variable."
    when: "When tracing seeded ownership across scripts/database files."
---

# Seed owner OID bootstrap

## Context

Use this when application logic starts resolving ownership from seeded
data and fresh databases need a bootstrap path. In JJGNet Broadcasting,
collectors now fail closed unless persisted source rows already have a
non-blank owner OID.

## Pattern

- Declare one owner variable near the top of `scripts/database/data-seed.sql`.
- Add a TODO comment telling operators to replace the placeholder GUID
  with a real Entra object ID for seeded ownership.
- Reuse that variable for every seeded insert that targets owner-aware
  tables in the bootstrap path.
- Prefer updating the base seed script over adding an EF migration or
  runtime fallback.
- Keep the fix narrow: solve fresh-database bootstrap without
  redesigning ownership resolution.

## Example

- `scripts\database\data-seed.sql`
- `.squad\decisions\inbox\morpheus-bootstrap-owner-oid-seed.md`

## Anti-Patterns

- Scattering different hard-coded owner GUIDs through the seed script.
- Leaving seeded `CreatedByEntraOid` values null or blank when
  fail-closed logic depends on them.
- Fixing only runtime code while leaving the clean-database bootstrap path broken.
