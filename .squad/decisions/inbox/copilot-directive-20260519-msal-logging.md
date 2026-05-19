### 2026-05-19T10:59:48: User directive
**By:** Joseph Guadagno (via Copilot)
**What:** Do NOT suppress MSAL/IdentityModel debug log output in DEBUG builds. The verbose logging is intentionally kept visible locally so that issues like L1 cache misses remain detectable. The three MinimumLevel.Override calls added for Microsoft.Identity, Microsoft.IdentityModel, and MSAL in LoggingExtensions.cs should be reverted.
**Why:** User preference — "if we had the MSAL excluded, we might not have found the issue"
