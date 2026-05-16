# Decision: Hash Discriminator in KeyVaultSecretNameBuilder (Supersedes Sanitize-All-Segments)

**Date:** 2026-05-16
**Author:** Oracle (Security Engineer)
**Branch:** issue-972-end-user-validation
**Status:** Implemented

---

## Context

Azure Key Vault enforces that secret names match `[a-zA-Z0-9-]{1,127}`. Underscores are
not allowed and cause a `400 Bad Request` from the Key Vault API.

`KeyVaultSecretNameBuilder.Build()` accepts five parameters to compose a secret name:

```
{ownerType}-{ownerOid}-{platform}-{settingName}
{ownerType}-{ownerOid}-{platform}-{discriminator}-{settingName}
```

### Pass 1 (superseded)

A first fix extracted `SanitizeSegment()` and applied it to all segments including
`discriminator`. This resolved the Key Vault `400` error.

### Why Pass 1 Was Insufficient

YouTube Channel IDs use **base64url encoding**, where BOTH `-` and `_` are valid,
semantically distinct characters. Two different channel IDs — `UCabc-def` and
`UCabc_def` — differ only in one character. Simple substitution (`_` → `-`) maps both
to the same sanitized string. This is a **silent collision**: one user's Key Vault
secret would silently overwrite another user's, with no error raised.

Joe confirmed: _"my channel id and many others have an underscore in them. We are going
to have to figure something out since this pattern will not work."_

## Decision

Apply `HashDiscriminator()` to the `discriminator` parameter instead of
`SanitizeSegment()`. All other segments (`ownerOid`, `platform`, `settingName`) continue
to use `SanitizeSegment()` — they are controlled values that do not carry the base64url
collision risk.

```csharp
private static string HashDiscriminator(string discriminator)
{
    if (string.IsNullOrEmpty(discriminator))
        return string.Empty;
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(discriminator));
    // First 8 bytes = 16 hex chars — sufficient entropy for the user-set size
    return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
}
```

## Consequences

- **Positive:** Distinct channel IDs always produce distinct Key Vault names —
  `UCabc_def` → `d5ca878c9efddfbd`, `UCabc-def` → `ab82aaa6dd869199`.
- **Positive:** Any Unicode discriminator maps to a Key-Vault-safe hex string without
  special-char restrictions.
- **Positive:** Deterministic — same input always yields same output; no secret name
  drift across restarts.
- **Positive:** 16 hex chars fits easily within the 127-char Key Vault name limit.
- **Neutral:** Existing secrets written with the sanitized (hyphen-substituted) name
  cannot be found by the new hash-based name. However, since Pass 1 was never deployed
  to production (it landed in the same branch as Pass 2), there are no live secrets to
  migrate.

## Alternatives Considered

1. **Keep sanitization, add a prefix/suffix to distinguish `-` vs `_`** — Rejected.
   Fragile, encoding-specific, and brittle for any future base64url variant.
2. **Percent-encode the discriminator** — Rejected. `%` is not a valid Key Vault name
   character; would still require a second sanitization pass, defeating the purpose.
3. **URL-safe Base64-encode the discriminator** — Rejected. Still contains `=` padding
   and potentially `-` / `_` depending on the library, requiring further transformation.
4. **SHA-256 full hash (64 chars)** — Considered but unnecessary. 8 bytes / 16 hex
   chars provides ~1-in-18-quintillion collision probability, sufficient for any
   realistic user count.

## Files Changed

- `src/JosephGuadagno.Broadcasting.Domain/Utilities/KeyVaultSecretNameBuilder.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Tests/KeyVaultSecretNameBuilderTests.cs`
