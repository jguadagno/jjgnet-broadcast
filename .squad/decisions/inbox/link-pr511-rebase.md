# Decision: PR #511 CI Fix — Merge main instead of rebase

**Date:** 2025-07-14  
**Author:** Link (Platform & DevOps Engineer)  
**PR:** #511 `feature/s8-328-wire-application-insights`

## Decision

Used `git merge origin/main --no-edit` (not rebase) to bring PR #511 up to date with main after PR #513 landed.

## Rationale

- PR #511's changes are entirely in `ServiceDefaults/` and `Program.cs` files — no overlap with the controller/test renames from PR #513.
- Merge produced a clean auto-merge with no conflicts.
- Rebase was unnecessary complexity for a non-overlapping change set; merge preserves the original commit history and is less risky in a shared branch.

## Workflow conflict policy (secondary decision)

When popping stashes onto branches that have received `origin/main` updates, workflow file conflicts in `.github/workflows/*.yml` should always resolve to the `origin/main` version. The vuln-scan policy (Critical-only gate, with High/Medium/Low logged but non-blocking) was deliberately established in PR #509 and must not be regressed.
