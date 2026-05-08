# Process Learnings

Self-learning file for the Dev Lifecycle skill. Captures codebase-specific patterns and
process debt discovered during dev cycles. Read at the start of every `/dev` invocation.

Entries are appended after each cycle. The 3x rule applies: when an entry reaches 3 occurrences,
surface it to the user and propose either a permanent convention or a skill process change.

---

## Codebase Patterns

<!-- Patterns specific to codebases worked on. Format:

### <Pattern Name> (project: <project>)
**Discovered:** YYYY-MM-DD
**Occurrences:** N
**Pattern:** <what to do>
**Why:** <what went wrong without it>

-->

### Use build-with-signing.ps1 for release builds (project: project-desk-app)
**Discovered:** 2026-03-24
**Occurrences:** 1
**Pattern:** Always run `powershell -ExecutionPolicy Bypass -File build-with-signing.ps1` instead of `npx tauri build` for release builds.
**Why:** The .env contains Azure Trusted Signing credentials that the PowerShell script loads into the process environment. Without them, the Rust build.rs panics on missing TS_ENDPOINT.

### Register new Tauri commands in invoke_handler (project: project-desk-app)
**Discovered:** 2026-03-24
**Occurrences:** 1
**Pattern:** After adding a `#[tauri::command]` function in any .rs file, immediately add it to the `invoke_handler![]` macro in lib.rs.
**Why:** Forgetting registration is a silent failure — the frontend invoke call will error at runtime with no compile-time warning.

### Empty .vue files break vite build (project: project-desk-app)
**Discovered:** 2026-03-24
**Occurrences:** 1
**Pattern:** Never leave a .vue file empty. Add at least `<template><div></div></template><script setup></script>` as a placeholder.
**Why:** Vite's Vue plugin requires at least one `<template>` or `<script>` block — an empty file causes a build error that blocks all other compilation.

---

## Process Debt

<!-- Inefficiencies in the dev lifecycle process itself. Format:

### <Debt Name>
**Discovered:** YYYY-MM-DD
**Occurrences:** N
**Problem:** <what was inefficient>
**Proposed fix:** <recommendation for next cycle>

-->

### Grill context lost when /dev invoked after separate grill session
**Discovered:** 2026-03-24
**Occurrences:** 1
**Problem:** The Grill phase was completed before `/dev` was invoked, so the skill had to reconstruct the resolved design from conversational context rather than having structured Grill outputs.
**Proposed fix:** Support an "attach prior grill" workflow — if the user says "skip grill, we already resolved the design", the skill should ask for a concise summary and format it as structured Grill output before proceeding.

### Plan phase should verify base branch dependencies
**Discovered:** 2026-03-24
**Occurrences:** 1
**Problem:** The Plan phase didn't check that the feature depends on code only available on feature/claude-dashboard (not master). This caused a mid-build branch switch that interrupted flow.
**Proposed fix:** During Plan, verify that all files to be modified exist on the current branch. If not, surface the dependency and ask the user to confirm the base branch before proceeding.

### Validate build toolchain before Ship phase
**Discovered:** 2026-03-24
**Occurrences:** 1
**Problem:** Missing .env signing vars weren't discovered until Ship phase, requiring a detour to find and restore them from a backup.
**Proposed fix:** At the start of Ship, run a pre-flight check: verify the build script exists, required env vars are set, and the build command is likely to succeed before attempting the full build.

---

## Promoted Conventions

<!-- Patterns that hit 3x and were promoted to permanent conventions.
These are "graduated" from the Codebase Patterns section above.

### <Convention Name> (project: <project>)
**Promoted:** YYYY-MM-DD
**Original occurrences:** N
**Convention:** <the permanent rule>

-->

_No entries yet. Conventions are promoted when a pattern hits 3 occurrences._
