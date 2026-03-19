# Oracle Decision Record: HTTP Security Headers Middleware (S6-6, Issue #303)

## Date
2026-03-19

## Author
Oracle (Security Engineer)

## Status
Pending Ghost review for CSP allowlist

---

## Context

Both the API and Web applications were missing standard HTTP security response headers, leaving
responses vulnerable to clickjacking, MIME sniffing, and cross-site scripting. Issue #303 requires
adding the full recommended header set to every response in both projects.

---

## Decisions

### 1. Implementation approach — inline `app.Use` middleware

Used `app.Use(async (context, next) => { ... })` in each `Program.cs` rather than a third-party
package (`NWebsec`, `NetEscapades.AspNetCore.SecurityHeaders`). Rationale: zero new dependencies,
the header set is small and stable, and the policy strings are clearly readable in one place. If
the policy grows significantly, migrating to `NetEscapades.AspNetCore.SecurityHeaders` is a low-cost
future refactor.

Middleware is placed **after** `UseHttpsRedirection()` so headers are only emitted on HTTPS
responses and are not duplicated on redirect responses.

### 2. Headers applied — API (`JosephGuadagno.Broadcasting.Api`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `DENY` | API has no legitimate iframe use; strictest setting |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy browser XSS auditor (superseded by CSP) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage on cross-origin navigation |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | API serves JSON only; no scripts/styles/frames needed. `frame-ancestors 'none'` reinforces DENY framing |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | Disable browser features not required by a REST API |

### 3. Headers applied — Web (`JosephGuadagno.Broadcasting.Web`)

| Header | Value | Rationale |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `SAMEORIGIN` | MVC app may legitimately frame its own pages (e.g. OAuth popups) |
| `X-XSS-Protection` | `0` | Modern recommendation: disable legacy XSS auditor |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage |
| `Content-Security-Policy` | See §4 below | |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | No browser hardware features used |

### 4. Web Content-Security-Policy rationale

**Policy:**
```
default-src 'self';
script-src 'self' cdn.jsdelivr.net;
style-src 'self' cdn.jsdelivr.net;
img-src 'self' data: https:;
font-src 'self' cdn.jsdelivr.net data:;
connect-src 'self';
frame-ancestors 'self';
object-src 'none';
base-uri 'self';
form-action 'self'
```

**Directive-by-directive rationale:**

- **`default-src 'self'`** — safe fallback; anything not explicitly listed must come from the
  same origin.
- **`script-src 'self' cdn.jsdelivr.net`** — `'self'` covers all local JS bundles (jQuery,
  Bootstrap, site.js, schedules.edit.js, theme-support.js, the two new externalized scripts).
  `cdn.jsdelivr.net` is required in production for jQuery, Bootstrap bundle, FontAwesome JS,
  jquery-validation, and FullCalendar. No `'unsafe-inline'` — inline scripts were externalized
  (see §5).
- **`style-src 'self' cdn.jsdelivr.net`** — `cdn.jsdelivr.net` required in production for
  Bootstrap CSS, Bootstrap Icons CSS, and FontAwesome CSS. No `'unsafe-inline'` — the one inline
  `<style>` block in Calendar.cshtml was moved to `site.css`.
- **`img-src 'self' data: https:`** — `'self'` covers `/favicon.ico` and local images.
  `data:` is required for Bootstrap Icons (inline SVG data-URIs in the CSS). `https:` covers
  `@Settings.StaticContentRootUrl` favicon images whose exact hostname is a runtime setting
  (see open question §6).
- **`font-src 'self' cdn.jsdelivr.net data:`** — `cdn.jsdelivr.net` for FontAwesome woff2/woff
  files. `data:` covers any base64-encoded font fallbacks in vendor CSS.
- **`connect-src 'self'`** — all XHR/fetch calls go to the same origin (Engagements calendar
  events endpoint, API calls proxied by the Web app).
- **`frame-ancestors 'self'`** — paired with `X-Frame-Options: SAMEORIGIN`; allows same-origin
  framing, denies cross-origin.
- **`object-src 'none'`** — no Flash/plugin content.
- **`base-uri 'self'`** — prevents base tag injection attacks.
- **`form-action 'self'`** — all form POSTs must target the same origin.

### 5. Inline script/style externalization

Two inline `<script>` blocks were moved to dedicated JS files to avoid needing `'unsafe-inline'`
in `script-src`:

- `Views/MessageTemplates/Index.cshtml` → `wwwroot/js/message-templates-index.js`
  (Bootstrap tooltip initializer)
- `Views/Schedules/Calendar.cshtml` → `wwwroot/js/schedules-calendar.js`
  (FullCalendar initializer; no server-side data injection — uses an AJAX endpoint)

One inline `<style>` block from `Views/Schedules/Calendar.cshtml` (`#calendar` sizing) was moved
to `wwwroot/css/site.css`.

### 6. Open Questions for Ghost Review

1. **`img-src https:`** — This broad allowance was chosen because `Settings.StaticContentRootUrl`
   (used for favicons) is a runtime configuration value with an unknown hostname at code-time.
   Ghost should evaluate whether this should be tightened to the known static asset host
   (e.g., `https://static.josephguadagno.net`) and potentially read from config at startup.

2. **`cdn.jsdelivr.net` scope** — All CDN assets are pinned with SRI `integrity=` hashes in
   the Production `<environment>` blocks. The CSP host allowance is a belt-and-suspenders
   measure. Ghost should confirm no other CDN hostnames are referenced in any partial views
   not covered by this review.

3. **Nonce-based CSP** — A future improvement would replace the `cdn.jsdelivr.net` allowance
   with per-request nonces, eliminating CDN host trust entirely. Out of scope for S6-6.

---

## Files Changed

- `src/JosephGuadagno.Broadcasting.Api/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/Program.cs` — security headers middleware added
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/message-templates-index.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/schedules-calendar.js` — new (externalized)
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/css/site.css` — calendar style appended
- `src/JosephGuadagno.Broadcasting.Web/Views/MessageTemplates/Index.cshtml` — inline script removed
- `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Calendar.cshtml` — inline script and style removed
