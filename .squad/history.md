# Project Context

- **Owner:** Joe (jguadagno)
- **Project:** JJGNet Broadcasting — multi-platform social media publisher
- **Stack:** C#, ASP.NET Core, Entity Framework Core, Azure, Azure Key Vault
- **Created:** 2025

## Learnings

### Azure Key Vault Pattern for API Key Storage (2026-05-11)
Sensitive API keys for external platforms (YouTube, etc.) should be stored in Azure Key Vault, not in the database. The database stores only the Key Vault secret name reference (e.g., `youtube-channel-apikey-{ownerOid}-{channelId}`). Domain models can carry a transient `ApiKey` field as a pipeline carrier between Web ViewModel and API controller, where the controller orchestrates the encryption → KV storage → retrieval flow. API responses expose a `HasApiKey` boolean instead of the raw key. This pattern is reusable for other external platform integrations.

<!-- Append new learnings below. Each entry is something lasting about the project. -->
