# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| latest  | :white_check_mark: |

## Reporting a Vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Report vulnerabilities privately via [GitHub's security advisory feature](https://github.com/jguadagno/jjgnet-broadcast/security/advisories/new)
or by emailing the maintainer directly.

You can expect an acknowledgment within 48 hours and a resolution timeline communicated within 5 business days.

---

## Known Addressed Vulnerabilities

### CVE-2026-26127 / GHSA-8fh9-c4jq-94h4 — Denial of Service via `Microsoft.Bcl.Memory`

| Field        | Detail |
|--------------|--------|
| **Severity** | High |
| **Advisory** | https://github.com/advisories/GHSA-8fh9-c4jq-94h4 |
| **Affected**  | `Microsoft.Bcl.Memory` (transitive via `idunno.AtProto` / `idunno.Bluesky` < 1.7.0) |
| **Fix**       | Upgrade `idunno.Bluesky` to **1.7.0** (removes the vulnerable transitive dependency) |
| **Status**    | ✅ Patched — `idunno.Bluesky` pinned to `1.7.0` in `JosephGuadagno.Broadcasting.Managers.Bluesky` |
| **PR**        | [#488](https://github.com/jguadagno/jjgnet-broadcast/pull/488) |

#### Background

`idunno.Bluesky` versions prior to 1.7.0 pulled in a vulnerable version of `Microsoft.Bcl.Memory`
as a transitive dependency through `idunno.AtProto`. The vulnerability allows a remote attacker
to trigger a Denial of Service condition. Upgrading `idunno.Bluesky` to 1.7.0 removes the
dependency on the affected `Microsoft.Bcl.Memory` version entirely.
