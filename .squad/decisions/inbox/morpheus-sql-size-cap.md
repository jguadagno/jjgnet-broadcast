# Decision: SQL Server Size Cap Removal and Error Surfacing

## Date
2026-03-21

## Issue
#324 — SQL Server 50MB database size cap causes silent INSERT failures

## Context
The database-create.sql script provisioned SQL Server with a hard 50MB cap on the data file (`MAXSIZE = 50`) and 25MB cap on the log file (`MAXSIZE = 25MB`). When these limits were hit, INSERT operations would silently fail without surfacing any error to the application layer, making debugging extremely difficult.

## Root Cause
1. **Provisioning constraint:** The database creation script had arbitrary size limits (likely remnants of LocalDB or Azure SQL free-tier constraints)
2. **Silent failure:** EF Core's SaveChangesAsync would not surface SQL error 1105 (insufficient space) as a meaningful exception, leaving the application unaware of capacity issues

## Decision

### 1. Remove Size Caps (Preventive)
Changed `scripts/database/database-create.sql`:
- Data file: `MAXSIZE = 50` → `MAXSIZE = UNLIMITED`
- Log file: `MAXSIZE = 25MB` → `MAXSIZE = UNLIMITED`

**Rationale:** The 50MB cap was arbitrary and inappropriate for a production-grade application. Modern SQL Server containers and Azure SQL tiers support much larger databases. UNLIMITED allows the database to grow as needed (subject to disk space and SQL Server edition limits).

### 2. Surface Capacity Errors (Defensive)
Added `SaveChangesAsync` override in `BroadcastingContext` to catch `DbUpdateException` with inner `SqlException` and check for error number 1105 (insufficient space):

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    try
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
    {
        if (sqlEx.Number == 1105)
        {
            throw new InvalidOperationException(
                "Database capacity exceeded. The database has reached its maximum size limit. " +
                "Contact the administrator to increase the database capacity or archive old data.",
                ex);
        }
        throw;
    }
}
```

**Rationale:** Even with UNLIMITED, capacity issues can still occur (disk full, quota limits). This ensures the application fails fast with a clear error message rather than silently swallowing INSERT failures.

### 3. Migration for Existing Databases
Created `scripts/database/migrations/2026-03-21-increase-database-size-limits.sql` using `ALTER DATABASE MODIFY FILE`, which updates existing databases without requiring recreation or data loss.

**Rationale:** Allows zero-downtime migration of existing databases. `MODIFY FILE` is non-destructive and can be run on live databases.

## Pattern Established
**Two-layer defense for database capacity issues:**
1. **Preventive:** Remove arbitrary limits in provisioning scripts unless there's a specific business or infrastructure constraint
2. **Defensive:** Override SaveChangesAsync in DbContext to catch and surface SQL errors that would otherwise fail silently

**SQL Error Handling in EF Core:**
- Wrap `DbUpdateException` and check `InnerException` for `SqlException`
- Check `SqlException.Number` for specific error codes (e.g., 1105 = insufficient space, 2627 = unique constraint violation)
- Throw domain-appropriate exceptions (e.g., `InvalidOperationException`, `ArgumentException`) with clear messages

## Alternatives Considered
1. **Increase cap to 500MB instead of UNLIMITED:** Rejected because it just delays the problem and adds complexity
2. **Add monitoring/alerting instead of error handling:** Rejected as insufficient — alerting is good but doesn't prevent silent failures
3. **Use EF Core interceptors instead of SaveChangesAsync override:** Considered but SaveChangesAsync override is simpler and sufficient for this use case

## Impact
- New databases provisioned via Aspire AppHost will have no size caps
- Existing databases can be migrated using the provided script
- INSERT failures due to capacity will throw clear exceptions visible in logs and monitoring
- No breaking changes to existing code

## Related
- PR #517
- Sprint 9 milestone
