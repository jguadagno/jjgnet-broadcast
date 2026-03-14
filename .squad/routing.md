# Squad Routing

## Work Type Rules

| Work Type | Primary Agent | Fallback |
|-----------|---------------|----------|

## Governance

- Route based on work type and agent expertise
- Update this file as team capabilities evolve

## PR Policy

- **All work must be delivered via a pull request** — no direct commits to `main`
- **One PR per issue** — each PR must be tied to exactly one issue; do not bundle multiple issues into a single PR
- Branch naming: use the issue number, e.g. `issue-271` or `feature/271-twitter-manager`
- PR title should reference the issue, e.g. `feat(#271): create TwitterManager abstraction`

## Work Type → Agent

| Work Type | Primary | Secondary |
|-----------|---------|----------|
| Architecture, decisions, review | Neo | — |
| API, Functions, business logic | Trinity | — |
| SQL Server, Table Storage, EF Core | Morpheus | — |
| xUnit, Moq, FluentAssertions | Tank | — |
| ASP.NET Core MVC Web, Razor, LibMan, CSS/JS | Switch | Trinity (API contracts) |
| Azure AD, OAuth2, Key Vault, secret management | Oracle | Trinity (API endpoints) |
| GitHub Actions, Aspire AppHost, Bicep, Azure deployment | Cypher | Neo (arch decisions) |
| Azure AD, Key Vault, OAuth2, token lifecycle, auth middleware | Ghost | Neo |
| GitHub Actions, Pulumi IaC, Aspire AppHost, Event Grid, deployment | Link | Neo |
| Razor views, LibMan, Bootstrap, JS/CSS, frontend | Sparks | Trinity |
