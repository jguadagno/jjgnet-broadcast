# PR Guardrails

## When to use

- You need to add or repair branch and PR governance in this repository.
- You need consistent local hook enforcement plus CI verification.

## Rules

- Branches must target exactly one issue:
  - `issue-<number>-<slug>`
  - `feature/<number>-<slug>`
- PR titles must follow `<type>(#<issue>): <summary>`
- PR bodies must include exactly one closing issue reference matching the branch
  and PR title

## Implementation pattern

1. Add `.githooks\pre-commit` to block direct commits to `main` and reject
   branch names without a single issue number.
2. Add `.githooks\commit-msg` to enforce Conventional Commits.
3. Add `scripts\setup-git-hooks.ps1` to configure `core.hooksPath` and the local
   commit template.
4. Add a PR-only CI job that validates branch name, PR title, and linked issue
   matching without changing push-to-main build/test behavior.
5. Update `CONTRIBUTING.md` and `.github\pull_request_template.md` so the local
   and CI rules are explicit.

## Why

Local hooks fail fast for contributors, but CI is still required because hooks
can be skipped or never installed. The branch name, PR title, and linked issue
must all agree so "one issue per PR" is enforced mechanically instead of by
memory.
