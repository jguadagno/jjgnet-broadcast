---
date: 2026-04-21
author: Ghost
issue: 764
pr: 801
status: completed
---

# Decision: Rebase PR #801 on main and keep Phase 0 dual enforcement intact

## Context

PR #801 was blocked after #800 merged because the branch was stale and the next revision cycle could not stay with Trinity under reviewer-lockout rules. The API RBAC foundation still needed a fresh validation pass on top of current `main`.

## Decision

Rebase `issue-764-api-rbac` on the latest `origin/main`, keep the Phase 0 API change additive, and validate the real DI wiring plus smoke-level claims transformation tests without replacing any existing scope checks in API controllers.

## Why

- Rebased history removes already-merged dependency noise from PR #800 and makes the remaining review surface explicit.
- Phase 0 is security-sensitive because changing scope enforcement early would weaken backward compatibility; keeping `VerifyUserHasAnyAcceptedScope(...)` in place preserves dual enforcement while role infrastructure lands.
- Infrastructure-level tests are the least risky proof point for this phase because they verify policy registration and shared claims transformation without prematurely coupling controller behavior to role policies.

## Validation

- `dotnet restore .\src\`
- `dotnet build .\src\ --no-restore --configuration Release`
- `dotnet test .\src\ --no-build --verbosity normal --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader"`
