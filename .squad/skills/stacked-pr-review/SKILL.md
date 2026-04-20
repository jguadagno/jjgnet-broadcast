# Stacked PR Review

## Purpose

Use this checklist when reviewing dependent pull requests in a stack.

## Rules

1. Review each PR against its declared base, not the imagined final squashed result.
2. Treat "downstream PR fixes upstream build/test break" as a blocking defect.
3. Treat unrelated file drift in a PR as blocking when it violates the one-PR-per-issue rule.
4. Call out explicit merge order and required retargeting after each merge.
5. Check clean-environment/bootstrap behavior separately from happy-path local state.

## Sprint 21 Example

- #770 is the base PR and must stand alone on `main`.
- #771 cannot remove `Settings.OwnerEntraOid` unless its own branch also updates the Functions tests that still compile against that type.
- #772 can add regression coverage on top of #771, but it cannot carry unrelated Web scope config changes.
