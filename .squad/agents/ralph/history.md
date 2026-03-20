# Ralph — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Work Monitor
- **Joined:** 2026-03-14T16:37:57.751Z

## Learnings

### Session 2026-03-20: Triage Audit Patterns
**Date:** 2026-03-20  
**Finding:** Zero untriaged backlog issues with `squad` label but no `squad:{member}` sub-label. All squad-tracked work is already routed.

**Triage routing patterns confirmed (Issue #198, #191, #170, #167):**
1. **EventGrid subscriber functions (#198)** → Trinity (owns Azure Functions, platform publishers)
2. **Privacy page content updates (#191)** → Sparks (owns Razor views, static content)
3. **Fine-grained OAuth2 scopes (#170)** → Ghost (owns OAuth2/OIDC, auth middleware)
4. **Database schema changes (#167 BlueSky handle)** → Morpheus (owns SQL, EF Core, domain models)

**Closed:** #200 already resolved (talks now populated from Presentations collection in SpeakingEngagementsReader.cs lines 76-93).

**Label system operational:** Created 11 squad labels (squad, squad:neo, squad:trinity, squad:morpheus, squad:tank, squad:switch, squad:sparks, squad:ghost, squad:oracle, squad:cypher, squad:link) for future routing.
