# Pull Request

## Description

Brief description of your changes.

## Related Issue

- Link exactly one issue with a closing keyword.
- Example: `Closes #774`
- Do not bundle multiple issues into one PR.

Closes #

## Pre-Submission Checklist

- [ ] My branch name follows the issue convention (`issue-<number>-<slug>` or `feature/<number>-<slug>`)
- [ ] My PR title follows `<type>(#<issue>): <summary>` and matches the linked
      issue above
- [ ] I ran `.\scripts\setup-git-hooks.ps1` so the local branch and commit hooks
      are active
- [ ] I ran `dotnet restore .\src\`
- [ ] I ran `dotnet build .\src\ --no-restore --configuration Release`
- [ ] I ran `dotnet test .\src\ --no-build --verbosity normal --configuration
      Release --filter "FullyQualifiedName!~SyndicationFeedReader"`
- [ ] Zero test failures — I have not included known failures in this PR
- [ ] This PR contains changes for exactly one issue
- [ ] No "Note on Remaining Test Failures" or similar acknowledgments in this
      description

## Type of Change

- [ ] Bug fix
- [ ] New feature
- [ ] Process/infrastructure change

## Additional Context
