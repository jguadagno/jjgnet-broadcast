---
date: 2026-04-21
author: Trinity
issue: 763
status: implemented
---

# Decision: Shared auth transformer in Managers needs ASP.NET Core framework reference

## Context

PR #800 moves `EntraClaimsTransformation` into
`JosephGuadagno.Broadcasting.Managers` so Web and API can share one
implementation. CI failed because the Managers class library did not
reference the ASP.NET Core shared framework that provides
`Microsoft.AspNetCore.Authentication` and `IClaimsTransformation`.

## Decision

Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to
`src\JosephGuadagno.Broadcasting.Managers\JosephGuadagno.Broadcasting.Managers.csproj`
and remove the stale Web-local `EntraClaimsTransformation` copy.

## Why

- The shared transformer now legitimately depends on ASP.NET Core auth
  abstractions.
- Keeping the old Web copy alongside the Managers copy creates an
  ambiguous `EntraClaimsTransformation` type in the Web host.
- This preserves the Sprint 22 decision that Managers is the canonical
  home for the shared claims transformation.

## Outcome

Release build and the normal CI-aligned test pass now succeed on
`issue-763-entra-extraction`.
