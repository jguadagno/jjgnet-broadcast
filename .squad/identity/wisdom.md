---
last_updated: {timestamp}
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

## C# Naming Conventions

### DI Constructor Parameter & Backing Field Naming

- **Constructor-injected parameter names:** camelCase (e.g., `syndicationFeedItemManager`)
- **Private backing fields:** `_camelCase` (e.g., `_syndicationFeedItemManager`)
- PascalCase parameters are non-conformant — `BlueskyManager.cs` is a known example to fix.

## Anti-Patterns

<!-- Things we tried that didn't work. **Avoid:** description. **Why:** reason. -->
