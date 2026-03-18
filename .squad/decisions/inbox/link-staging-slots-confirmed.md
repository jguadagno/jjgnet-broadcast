# Decision: Staging Slots Confirmed Active

**Date:** 2026-03-18
**Author:** Link (Platform & DevOps Engineer)

## Context

My charter listed "No staging deployment slot or approval gate — every push to `main` goes straight to production" as a known issue. This has been resolved.

## Current State

All three Azure deployment targets have active staging slots:

| Service | App Name | Staging Slot |
|---|---|---|
| Azure Functions | `jjgnet-broadcast` | `jjgnet-broadcast-staging` |
| API App Service | `api-jjgnet-broadcast` | `api-jjgnet-broadcast-staging` |
| Web App Service | `web-jjgnet-broadcast` | `web-jjgnet-broadcast-staging` |

All three GitHub Actions workflows (`main_jjgnet-broadcast.yml`, `main_api-jjgnet-broadcast.yml`, `main_web-jjgnet-broadcast.yml`) already implement the correct 3-job pattern:

1. **build** — compiles, tests, publishes artifact
2. **deploy-to-staging** — deploys artifact to the staging slot
3. **swap-to-production** — runs under the `production` GitHub environment (approval gate), then performs an Azure slot swap

## Required GitHub Secret

All three `swap-to-production` jobs reference `${{ secrets.AZURE_RESOURCE_GROUP }}`. Confirm this secret is set in the repository.

## Known Issue Resolved

The "no staging slot" known issue in my charter is now closed. No pipeline changes are needed.
