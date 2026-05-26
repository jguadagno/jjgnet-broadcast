### 2026-05-26: Issue #995 schema review

**By:** Trinity

**What:** Recommended that the new per-user scheduling/routing tables
use `CreatedByEntraOid nvarchar(36)`, `datetimeoffset` timestamps,
platform FKs, and normalized junction tables; also flagged that
`UserPublisherSchedules` likely needs `TimeZoneId` unless Joseph
declares all cron schedules UTC-only.

**Why:** This matches the existing `UserPublisher*Settings` /
`UserCollector*` ownership pattern in `Data.Sql` and avoids baking
publisher names or numeric user IDs into new routing tables. The
timezone choice changes the table shape and schedule recalculation
logic, so it needs to be settled before implementation starts.
