# Dev Log

Structured metrics log for the Dev Lifecycle skill. One entry per completed (or aborted)
dev cycle. Used for pattern detection, retrospective analysis, and self-learning.

Read at the start of every `/dev` invocation to detect recurring patterns via the 3x rule.

---

<!-- Entry format:

## YYYY-MM-DD | <project> | <feature/fix name>

- **Total duration:** Xmin (execution: Ymin, checkpoint-wait: Zmin)
- **Phase breakdown:** Grill: Xmin, Plan: Xmin, Scaffold: Xmin, Build: Xmin, Test: Xmin, Observe: Xmin, Review: Xmin, Ship: Xmin
- **Phases run:** (list phases that were included in this cycle)
- **Checkpoints hit:** N (list which phase, result: approved / approved-with-revision / rejected)
- **Files touched:** N new, N modified, N deleted
- **Issues caught at checkpoint:** (list — what phase, what issue)
- **Process debt identified:** (list — feeds into process-learnings.md)
- **Codebase patterns learned:** (list — feeds into process-learnings.md)
- **Outcome:** Merged / PR created / Aborted — reason
- **Notes:** (anything notable about this cycle)

-->

## 2026-03-24 | project-desk-app | Path Navigator

- **Total duration:** ~45min (execution: ~30min, checkpoint-wait: ~15min)
- **Phase breakdown:** Grill: skipped (pre-resolved), Plan: 5min, Scaffold+Build: 18min, Test: skipped, Observe: skipped, Review: 3min, Ship: 4min
- **Phases run:** Plan, Scaffold+Build (combined), Review, Ship
- **Checkpoints hit:** 3 (Plan: approved, Review: approved, Ship: approved)
- **Files touched:** 1 new (PathNavigator.vue), 4 modified (lib.rs, CellLauncher.vue, TerminalHeader.vue, TerminalMatrix.vue), 1 fixed (TeamMemberRow.vue placeholder)
- **Issues caught at checkpoint:** None — clean pass
- **Process debt identified:**
  - Grill was done in a separate conversation segment before `/dev` was invoked — had to reconstruct resolved design from context. Skill should support "attach prior grill" workflow.
  - Build required branch switch from master to feature/claude-dashboard — the feature depended on code not yet on master. Plan phase should verify base branch dependencies earlier.
  - .env was missing signing vars, blocking the release build. Discovered mid-Ship. Build script dependency should be validated before Ship phase starts.
- **Codebase patterns learned:**
  - In project-desk-app, always use `build-with-signing.ps1` for release builds — it loads .env vars that `npx tauri build` alone cannot access.
  - New Tauri commands must be registered in the `invoke_handler![]` macro in lib.rs — easy to forget.
  - Empty .vue files (like TeamMemberRow.vue) break `vite build` — always add at least a minimal `<template>` tag.
- **Outcome:** Pushed to origin/feature/path-navigator, installed v0.3.0 locally
- **Notes:** First `/dev` cycle. Grill was pre-resolved in the same conversation. Scaffold and Build were combined since it was a UI feature with one vertical slice. The 1M context window held the full design discussion + implementation seamlessly.
