# Decision: UI Dropdown Value Fix (Issue #274)

**Date:** 2025-01-16  
**Author:** Sparks (Frontend Developer)

## Decision

Updated `ItemTableName` dropdown option values in Schedule Add/Edit views and the supporting JS switch statement to match the backend's expected table name strings.

## Changes Made

| File | Change |
|------|--------|
| `Views/Schedules/Add.cshtml` | `value="SyndicationFeed"` → `"SyndicationFeedSources"`, `value="YouTube"` → `"YouTubeSources"` |
| `Views/Schedules/Edit.cshtml` | Same as above |
| `wwwroot/js/schedules.edit.js` | Updated `case` strings to match new values |

## Rationale

Display labels are user-facing and remain unchanged ("Syndication Feed", "YouTube"). Only the submitted `value` attributes were corrected to align with what Azure Functions collectors expect when looking up items in table storage.

## Outcome

Build passes (0 errors). Committed on branch `issue-274`.
