# Squad Routing

## Governance

- Route based on work type and agent expertise
- Update this file as team capabilities evolve

## PR Policy

- **All work must be delivered via a pull request** — no direct commits to `main`
- **One PR per issue** — each PR must be tied to exactly one issue; do not bundle multiple issues into a single PR
- Branch naming: use the issue number, e.g. `issue-271` or `feature/271-twitter-manager`
- PR title should reference the issue, e.g. `feat(#271): create TwitterManager abstraction`

## Work Type → Agent

| Work Type | Primary | Secondary | Notes |
|-----------|---------|-----------|-------|
| Architecture, decisions, review | Neo | — | |
| API endpoints, Azure Functions, business logic | Trinity | — | |
| SQL Server, Table Storage, EF Core | Morpheus | — | |
| xUnit, Moq, FluentAssertions tests | Tank | — | |
| MVC controllers, ViewModels, Web project structure | Switch | Trinity | API contracts only |
| Razor views, LibMan, Bootstrap, JS/CSS, static assets | Sparks | Switch | Views only, not controllers |
| OAuth2 / OIDC flows, token lifecycle, auth middleware, MSAL | Ghost | Neo | End-to-end auth implementation |
| Azure AD app registrations, Key Vault secrets, secret rotation | Oracle | Ghost | Secret management & policy |
| GitHub Actions CI/CD pipelines, Pulumi IaC, Event Grid, Azure resource deployment | Link | Neo | Full infra-as-code ownership |
| .NET Aspire AppHost, Bicep, local dev orchestration, container config | Cypher | Link | Local + staging environment config |
