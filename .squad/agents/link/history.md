# Link — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2025-07-14 | Fix PR #511 CI — merge main into `feature/s8-328-wire-application-insights` to pick up PR #513 test renames | ✅ Clean merge, pushed successfully. Also resolved workflow conflict in `feature/s8-315-api-dtos` stash pop (kept origin/main Critical-only vuln gate). |

## Learnings

- **Branch-behind-main CI failures**: When a PR branch pre-dates a merge to main that renames symbols (controllers, methods), the CI merge commit will fail to compile. Fix is always `git merge origin/main --no-edit` on the feature branch — no rebase needed when the changes are non-overlapping.
- **Stash pop conflicts in shared workflow files**: Workflow files (`.github/workflows/*.yml`) change frequently across branches. When popping a stash onto a branch that has received main updates, expect conflicts in workflow steps. Always favour the `origin/main` version of vuln-scan logic since those decisions are made intentionally and tracked in PRs.
- **Stash hygiene**: After a conflicted stash pop, git leaves the stash entry in the list. Always `git stash drop stash@{N}` after manually committing the resolution.
