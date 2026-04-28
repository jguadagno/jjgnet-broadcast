# Decision: Scriban rendering in email notification Functions (Issue #853)

**Date:** 2026-05-02
**Author:** Trinity
**PR:** #891 (`issue-853-notify-expiring-linkedin-tokens`)
**Issue:** #853

---

## Context

Issue #853 required the LinkedIn expiry notification Function to send personalised emails containing the user's display name, token expiration date, and a re-auth URL. Email templates are stored in the `EmailTemplates` SQL table as raw HTML bodies.

## Decision 1 — First use of Scriban rendering for email templates

**Decision:** Render Scriban markup inside the `EmailTemplates.Body` column before passing to `IEmailSender.QueueEmail()`.

**Rationale:** The existing `UserApproved` and `UserRejected` templates send static HTML with no variable substitution. For the LinkedIn expiry notifications, personalisation is required (`{{ display_name }}`, `{{ expires_at }}`, `{{ reauth_url }}`). Rather than adding a new abstraction, the simplest approach is to call `Template.Parse` + `template.Render` in the Function itself, falling back to the raw body string if Scriban fails. This keeps the template storage mechanism unchanged.

**Implication for future teams:** Any email template that needs variable substitution can now use Scriban syntax in its `Body` column. Functions/code that send those templates are responsible for rendering before queuing. `IEmailTemplateManager` does NOT render Scriban — the caller does.

---

## Decision 2 — `reauth_url` hardcoded as `/LinkedIn`

**Decision:** The `reauth_url` variable injected into the Scriban context is the relative path `/LinkedIn`.

**Rationale:** The Web app has a `/LinkedIn` controller route that initiates the LinkedIn OAuth flow. The full base URL is not known to the Functions project (it differs per environment). Since the email ultimately lands with the user who can click it in their browser against the same Web host they used to sign in, a relative URL is sufficient for now. If emails are opened in a different context (e.g., mobile client), this will need revisiting.

**Implication:** A future change could inject the Web base URL via configuration (e.g., `WebBaseUrl` app setting) and produce a full URL. That enhancement is not in scope for #853.

---

## Decision 3 — Deduplication granularity: once per UTC calendar day

**Decision:** Skip notification if `token.LastNotifiedAt.Value.UtcDateTime.Date >= todayUtc` (where `todayUtc = from.UtcDateTime.Date`).

**Rationale:** Prevents re-queuing the same email if the Function is retried or runs twice in a day. Resets on the next UTC calendar day so users receive a fresh reminder if the token remains un-renewed.
