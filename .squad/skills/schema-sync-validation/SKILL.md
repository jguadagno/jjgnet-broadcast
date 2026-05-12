# Skill: Schema-Sync Validation

## When to Use

Invoke this skill whenever any of these conditions apply:

- **New column added** to any existing table (SQL migration written)
- **New table created** (initial SQL + all four layers must be wired simultaneously)
- **Suspecting drift** — a column exists in SQL but a property is silently dropped during EF mapping, or a property exists in C# with no corresponding SQL column (EF will error at runtime or silently skip data)
- **Before closing any migration task** — run as the final sign-off gate
- **After a column rename or type change** — all four layers plus the mapper must be updated atomically

---

## Layer Inventory

Four layers must be in sync for every persisted column:

| Layer | Location |
|-------|----------|
| **SQL DDL (baseline)** | `scripts/database/table-create.sql` |
| **SQL DDL (migration)** | `scripts/database/migrations/YYYY-MM-DD-description.sql` |
| **EF entity model** | `src/JosephGuadagno.Broadcasting.Data.Sql/Models/<Entity>.cs` |
| **EF fluent configuration** | `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` |
| **Domain model** | `src/JosephGuadagno.Broadcasting.Domain/Models/<Entity>.cs` |
| **AutoMapper profile** | `src/JosephGuadagno.Broadcasting.Data.Sql/MappingProfiles/<Profile>.cs` |
| **Data store `SaveAsync`** | `src/JosephGuadagno.Broadcasting.Data.Sql/<Entity>DataStore.cs` |

> The SQL DDL baseline (`table-create.sql`) is the authoritative source of truth.  
> Migrations are additive patches for **existing** environments only.  
> Aspire bootstraps fresh environments from `table-create.sql` — NOT from replaying migrations.

---

## Column-by-Column Checklist

For **every column** in the SQL `CREATE TABLE` block, verify all of the following:

### 1 — SQL DDL (baseline + migration)

- [ ] Column present in `table-create.sql` with the correct name, type, length, nullability, and constraints
- [ ] `table-create.sql` baseline reflects the **cumulative final state** (it is updated alongside every migration)
- [ ] A migration file exists in `scripts/database/migrations/` for every schema change to an existing table
- [ ] Migration is **idempotent** (`IF NOT EXISTS` / `IF EXISTS` guards on all DDL statements)
- [ ] Migration uses `USE JJGNet; GO` header and `GO` after each DDL batch
- [ ] Date/time columns use `datetimeoffset` — never `datetime` or `datetime2`
- [ ] String columns use `nvarchar(N)` — never `varchar`

### 2 — EF Entity Model

- [ ] Property exists with the correct C# type
- [ ] `[MaxLength(N)]` data annotation matches the SQL `nvarchar(N)` length
- [ ] Nullable SQL column → nullable C# type (`string?`, `int?`, `DateTimeOffset?`)
- [ ] NOT NULL SQL column → non-nullable C# type with a default value (`string.Empty`, `0`, `false`, etc.)
- [ ] `DateTimeOffset` used for all date/time columns (not `DateTime`)
- [ ] **No data annotations that contradict the fluent config** — if both exist, fluent wins, but mismatched annotations create confusion and can mislead code review

### 3 — EF Fluent Configuration (BroadcastingContext)

- [ ] Property is explicitly configured (or is covered by a safe EF convention)
- [ ] `.HasMaxLength(N)` matches SQL `nvarchar(N)` — this is the definitive length constraint, not the data annotation
- [ ] `.IsRequired()` set for NOT NULL columns; `.IsRequired(false)` only for nullable columns
- [ ] `.HasColumnType("datetimeoffset")` on all `DateTimeOffset` properties
- [ ] No `.HasDefaultValueSql()` on value types (`bool`, `int`) — use `.HasDefaultValue(literal)` or omit (EF convention)
- [ ] Unique indexes declared with `.HasIndex(...).IsUnique()`
- [ ] Nonclustered owner indexes declared with `.HasIndex(e => e.CreatedByEntraOid, "IX_...")`
- [ ] PK configured with `.IsClustered()` (these tables use clustered PKs)

### 4 — Domain Model

- [ ] Property exists with the correct C# type
- [ ] `DateTimeOffset` used for all date/time columns
- [ ] Nullable SQL column → nullable C# type
- [ ] NOT NULL SQL column → non-nullable C# type with matching default value
- [ ] **Transient-only properties** (not in SQL) are documented as non-persisted in XML comments

### 5 — AutoMapper Profile

- [ ] Both directions mapped (`.ReverseMap()`)
- [ ] Transient domain-model properties (not in SQL) are explicitly `.Ignore()`d
- [ ] No SQL-backed column is ignored unless intentional (verify intent with data store author)

### 6 — Data Store `SaveAsync`

- [ ] Every **mutable** column (not PK, not IDENTITY, not audit-only `CreatedOn` / `CreatedByEntraOid`) is assigned on both insert and update paths
- [ ] `CreatedOn` is set **only on insert** (with `DateTimeOffset.UtcNow`)
- [ ] `LastUpdatedOn` is set on **every save** (both insert and update)
- [ ] `CreatedByEntraOid` is set **only on insert** (owner is immutable after creation)
- [ ] New columns added in a migration are wired into `SaveAsync` — forgetting this causes silent data loss

---

## Common Drift Patterns

These are patterns observed in this codebase. No specific bug is named; these are recurring traps.

### 1 — Data annotation / fluent config length mismatch
The EF entity carries a `[MaxLength(N)]` annotation, but the fluent config has a different `.HasMaxLength(M)`. EF uses the fluent config, so the annotation is wrong and misleads reviewers. Fix: make the annotation match the fluent config length.

### 2 — `IsRequired(false)` on a NOT NULL column
A column is `NOT NULL` in SQL but the fluent config uses `.IsRequired(false)`. EF Core treats the property as optional, can generate incorrect SQL for inserts (omitting the column), and emits incorrect scaffolding if re-generated. Fix: `.IsRequired()` for NOT NULL, `.IsRequired(false)` only for nullable.

### 3 — Column added to `table-create.sql` without a migration
A new column is added directly to `table-create.sql` without creating a corresponding migration file. Fresh environments get the column; existing environments silently miss it. Fix: always create a migration for every schema change to existing tables, even if the column already appears in `table-create.sql`.

### 4 — Migration created to drop a column that was never added via migration
The inverse of pattern 3: a DROP migration is written but the corresponding ADD migration was never created (or was added directly to the database). The DROP migration should be guarded with `IF EXISTS` to be idempotent. Without the guard it will fail on environments that never had the column. Fix: always add `IF EXISTS` guards to DROP statements; audit the migration chain for the missing ADD step.

### 5 — Mutable column omitted from `SaveAsync`
A new column is added to all four model layers but the `SaveAsync` method is not updated. Data is silently dropped on every write. This is the hardest drift to catch in review because no build error is emitted. Fix: run the column checklist against `SaveAsync` explicitly.

### 6 — Transient domain property missing `Ignore()` in mapper
A domain model carries a property that is NOT in the SQL schema (e.g., a computed field set at query time by a manager). If the mapper does not `.Ignore()` it, and the reverse map tries to project it back to the EF entity, EF may see unexpected state or attempt to write it. Fix: `.Ignore()` all transient properties in both directions of the map.

### 7 — `HasDefaultValue(literal)` used on non-trivial SQL defaults
`.HasDefaultValue()` is used for simple constants (`50`, `""`, `true`). It should NOT be used for SQL expressions like `sysdatetimeoffset()` or `getutcdate()` — those require `.HasDefaultValueSql("...")`. However, for audit fields like `CreatedOn` and `LastUpdatedOn`, the pattern in this codebase is to set these explicitly in C# (`DateTimeOffset.UtcNow`) rather than relying on the SQL default, so no `.HasDefaultValueSql()` is needed.

---

## Sign-Off Criteria

A migration task may be closed as **In Sync** only when ALL of the following are true:

1. `table-create.sql` contains the final schema with all new columns (baseline is updated)
2. A migration file exists in `scripts/database/migrations/` for every column add/drop/rename
3. Migration is idempotent (all DDL is guarded with `IF [NOT] EXISTS`)
4. EF entity model has a property for every SQL column with matching type and maxlength
5. EF fluent config declares `HasMaxLength`, `IsRequired`, and `HasColumnType` correctly for every property
6. Domain model mirrors the EF entity for all persisted columns
7. Transient domain properties are `Ignore()`d in the AutoMapper profile
8. `SaveAsync` assigns every mutable column in both insert and update paths
9. No C# property on the EF entity lacks a corresponding SQL column (EF runtime error risk)
10. No SQL column lacks a corresponding C# property (silent data loss risk)
