---
name: "shared-auth-component-placement"
description: "Place reusable auth components in the shared layer without breaking ASP.NET Core consumers or tests."
domain: "authentication"
confidence: "high"
source: "earned"
---

# Shared Auth Component Placement

## Context

Use this when an authentication or authorization component is being
extracted from a host project into a shared library such as Managers.

## Patterns

- Put reusable claims transformation logic in
  `JosephGuadagno.Broadcasting.Managers` instead of duplicating it per
  host.
- If the shared component implements ASP.NET Core auth contracts like
  `IClaimsTransformation`, add
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to that
  class library.
- Remove the old host-local implementation after extraction so the host
  resolves a single canonical type.
- Update tests to import the shared namespace explicitly when the type
  no longer lives in the host project.

## Examples

- `src\JosephGuadagno.Broadcasting.Managers\EntraClaimsTransformation.cs`
- `src\JosephGuadagno.Broadcasting.Managers\JosephGuadagno.Broadcasting.Managers.csproj`
- `src\JosephGuadagno.Broadcasting.Web.Tests\EntraClaimsTransformationTests.cs`

## Anti-Patterns

- Leaving the extracted component in both the shared library and the
  original host project.
- Moving an ASP.NET Core auth type into a class library without adding
  the required framework reference.
- Letting tests compile against a host-local copy when the shared
  implementation is supposed to be canonical.
