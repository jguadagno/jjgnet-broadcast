# Cypher — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** DevOps Engineer
- **Joined:** 2026-03-14T16:55:20.779Z

## Learnings

<!-- Append learnings below -->

### Approval Gates via GitHub Environments (2026-03-14)

- `environment: production` on a job is sufficient to create an approval gate in GitHub Actions. GitHub will pause the job and wait for a required reviewer **if** the `production` environment in GitHub Settings has required reviewers configured. No extra YAML is needed beyond the `environment:` key.
- The environment protection rules live entirely in GitHub Settings (not in YAML). A workflow can have `environment: production` in place before reviewers are added; enabling enforcement is a one-click change in Settings → Environments.
- After a slot swap, App Service staging slots can be stopped with `az webapp stop --slot staging`. For Azure Functions slots, use `az functionapp stop --slot staging`. Both commands accept `--name` and `--resource-group` identical to other az CLI calls in the workflow — no extra credentials needed since the same Azure login session covers all steps in the job.
- Stopping the staging slot after a swap is a cheap guard: it ensures staging is never left running/serving traffic after going dark, and makes the next staging deploy a clean start.

### Deployment Approval Gate + Staging Cleanup (2026-03-22)

- **Issue #556** / **PR #557**: Implemented production approval gate + automatic staging slot cleanup in all three workflows (API, Web, Functions)
- Added "Stop staging slot" step to each `swap-to-production` job in `.github/workflows/`
- GitHub environment gates already in place (`environment: production`) — required reviewers needed in GitHub Settings
- Scribe created orchestration logs and merged decisions into team decisions.md
- Neo flagged non-blocking observation: cleanup steps should run immediately after primary action (swap), before informational steps, to avoid skipping if downstream steps fail


## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only