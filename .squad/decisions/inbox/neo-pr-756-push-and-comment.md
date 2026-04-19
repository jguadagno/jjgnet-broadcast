---
date: 2026-04-19
author: Neo
pr: 756
branch: issue-731-user-publisher-settings
status: pushed-and-commented
---

# Decision: Recover #731 directly onto the existing PR branch and record merge readiness as a comment

## Context

The repaired issue #731 code existed only in a dirty local branch (`neo/pr-recovery-731-732`), while PR #756 still pointed at `issue-731-user-publisher-settings`. We needed to move the corrected product files without disturbing unrelated local work, then leave a visible GitHub note for the team.

## Decision

Use a dedicated git worktree for `issue-731-user-publisher-settings`, copy the recovered issue #731 files into that worktree, validate with the repo-wide Release build/test pass, commit with a Conventional Commit message, push to `origin/issue-731-user-publisher-settings`, and leave a regular PR comment on PR #756 instead of a formal review.

## Why

- The worktree preserves the original dirty recovery branch intact.
- Pushing onto the existing PR branch keeps PR #756 as the single integration point for issue #731.
- A normal PR comment is the correct visible review artifact when the PR is under the same GitHub user account and a formal approval review is inappropriate.
