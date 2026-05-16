# Decision: Sanitize All Segments in KeyVaultSecretNameBuilder

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

The `ownerOid` parameter was sanitized via a compiled `Regex([^a-zA-Z0-9\-])`. The
`discriminator` parameter — an optional segment used for YouTube Channel IDs — was
concatenated **without sanitization**. YouTube Channel IDs are user-supplied and can
contain underscores (e.g. `UC_my_channel`), triggering Key Vault errors when users
saved YouTube collector settings.

## Decision

Extract a `SanitizeSegment()` private static method wrapping the existing regex, and
apply it to **every string component** that feeds into the secret name:

- `ownerOid` (was already sanitized via inline call — now uses the helper)
- `platform` (defensive; currently always a `KeyVaultSecretNames.Platform` constant)
- `settingName` (defensive; currently always a `KeyVaultSecretNames.SettingName` constant)
- `discriminator` (the confirmed bug source — was completely unsanitized)

## Consequences

- **Positive:** YouTube channel IDs with underscores are now accepted; Key Vault
  creation no longer fails for those users.
- **Positive:** All four string inputs are uniformly sanitized at a single abstraction
  boundary — callers never need to pre-sanitize.
- **Positive:** Future callers who pass non-constant `platform` or `settingName` values
  are automatically protected.
- **Neutral:** The sanitized names for existing secrets with underscores will differ
  from any previously-stored names (underscores → hyphens). This is only relevant for
  secrets that were successfully stored before — those would have required a Key Vault
  version that accepted underscores, which standard Azure Key Vault does not. In
  practice, no such secrets exist in production because Key Vault was rejecting them.

## Alternatives Considered

1. **Sanitize only `discriminator`** — Rejected. The defensive approach costs nothing
   and prevents a future regression if `platform` or `settingName` ever receive
   non-constant values.
2. **Validate and throw on illegal input** — Rejected. Replacing illegal characters
   with hyphens preserves the secret's readability and is consistent with the
   established pattern already applied to `ownerOid`.
3. **Fix at call site in `UserCollectorYouTubeChannelManager`** — Rejected. The builder
   is the right place for this contract; callers should not need to know the Key Vault
   naming rules.

## Files Changed

- `src/JosephGuadagno.Broadcasting.Domain/Utilities/KeyVaultSecretNameBuilder.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Tests/KeyVaultSecretNameBuilderTests.cs`
