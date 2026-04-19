---
name: "git-pr-recovery"
description: "Safely push recovered issue-scoped fixes onto an existing PR branch without disturbing a dirty local workspace."
domain: "git-workflow"
confidence: "high"
source: "earned"
---

## Context

Use this when the right code already exists in a dirty local branch, but the real PR branch on origin is missing those fixes and must be updated without overwriting unrelated local work.

## Patterns

- Create or reuse a dedicated worktree for the target PR branch instead of checking it out in the dirty workspace.
- Copy only the issue-scoped files into the worktree, then validate there with the repo's normal build/test command before committing.
- If the recovery also needs a visible review note and the PR is effectively under the same GitHub user account, post a regular PR comment instead of a formal approval review.
- Record the recovery decision in .squad/decisions/inbox/ so the team knows the branch was repaired intentionally.

## Examples

- Source branch with repaired files: `neo/pr-recovery-731-732`
- Target PR branch: `issue-731-user-publisher-settings`
- Temporary safe workspace: `.worktrees\issue-731-recovery`
- Visible GitHub note: PR #756 comment confirming recovery push + merge readiness

## Anti-Patterns

- Do not switch the dirty workspace to the PR branch just to move files.
- Do not discard uncommitted recovery work after the push; preserve it until the branch lands.
- Do not submit a formal PR approval when protocol calls for a plain comment instead.
