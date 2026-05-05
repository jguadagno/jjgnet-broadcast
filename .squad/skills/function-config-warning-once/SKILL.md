---
name: "function-config-warning-once"
description: "Warn once per function run when optional URL configuration is
  missing, while keeping a safe fallback path."
domain: "error-handling"
confidence: "high"
source: "earned from issue #893 / PR #910"
tools:
---

# Function Config Warning Once

## Context

Use this pattern when a Function depends on optional configuration to build
links or other operator-visible output, but can still proceed in a degraded
mode.

## Patterns

- Read and normalize the configuration value once at function entry, not inside
  each item-processing loop.
- Treat `null`, empty, and whitespace-only values as missing configuration.
- Emit a single warning for the run so operators can spot misconfiguration
  without noisy repeated log entries.
- Return a stable fallback value (for example, an empty base URL that yields a
  relative path) and pass it into downstream methods.

## Examples

- `src\JosephGuadagno.Broadcasting.Functions\LinkedIn\NotifyExpiringTokens.cs`
  - `GetWebBaseUrl()` trims the configured URL, warns once when missing, and
    returns `string.Empty`
  - `RunAsync()` resolves the value once and passes it into both notification
    windows
- PR #910 applies the pattern without changing the existing relative-link
  fallback behavior
- `src\JosephGuadagno.Broadcasting.Functions.Tests\LinkedIn\NotifyExpiringTokensTests.cs`
  - `RunAsync_WhenWebBaseUrlIsMissingOrEmpty_LogsWarningAndUsesRelativeLink(...)`
    proves the warning is emitted for null/empty/whitespace settings while
    preserving the relative-link fallback

## Anti-Patterns

- Looking up the same config setting inside every per-user send path
- Logging one warning per token or email when the root cause is a single
  missing app setting
- Silently falling back to relative links with no operator-visible warning
