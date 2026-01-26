---
description: "Make a change to the SQL database schema."
argument-hint: "[description of change]"
---
Use the `@sql-schema` skill to apply the following database change: $ARGUMENTS.

Remember to modify the SQL scripts in `./scripts/` and update `AppHost.cs` if necessary. Do not use EF Migrations.
