# Sparks — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-03-20 | Added CodeQL analysis to ci.yml (#326) | ✅ CodeQL job added as separate job with csharp language, push to main trigger added to workflow |

## Learnings

### CodeQL Integration in CI
- CodeQL works best as a separate job with its own permissions (security-events: write)
- For .NET 10 preview, match the dotnet-quality: 'preview' setting from existing jobs
- CodeQL requires a build step for compiled languages like C#; used `dotnet build src/ --no-incremental`
- Added push to main trigger to ensure CodeQL runs on both PRs and main branch commits
- Vulnerable package scanning was already implemented with Critical CVE failure threshold
