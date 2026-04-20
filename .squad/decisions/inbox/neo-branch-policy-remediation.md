### 2026-04-20T12:15:00-07:00: Branch/PR Policy Remediation Pattern
**By:** Neo (Lead)
**What:** When work is accidentally committed to main, use stash-and-split to safely separate changes into issue branches without data loss.
**Why:** The team directive (`.squad/routing.md`) requires all work via PRs with one PR per issue. Direct main commits violate this. The remediation pattern ensures no work is lost while restoring compliance.

## Pattern

1. **Stash all uncommitted changes** with a descriptive message
2. **Create branches from origin/main** (not local main)
3. **Pop stash and selectively stage** files belonging to each issue
4. **Use stacked PRs** when issues have dependencies (base PR N+1 on branch N)
5. **Leave local main intact** — squad docs can stay local without affecting product branches

## Sprint 21 Application

- Stash: `Sprint21-uncommitted-work-backup`
- Branch chain: `issue-761` → `issue-760` → `issue-762`
- PR chain: #770 (base: main) → #771 (base: issue-761) → #772 (base: issue-760)
- Merge order: #770 first, then #771 (retarget to main), then #772 (retarget to main)

## Prevention

This is the **third** violation. The directive exists in `.squad/routing.md` and was reinforced in `.squad/decisions/inbox/copilot-directive-20260420-114527.md`. Agents must read decisions.md before starting work.
