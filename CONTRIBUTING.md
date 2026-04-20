# Contributing Guidelines

Thank you for contributing! This project follows the Conventional Commits
specification to keep the commit history clean and meaningful.

## Branch and Pull Request Policy

- All work must ship through a pull request. Do not commit directly to `main`.
- Keep each branch and pull request tied to exactly one issue.
- Name branches with the issue number:
  - `issue-774`
  - `issue-774-pr-guardrails`
  - `feature/774-pr-guardrails`
- Title pull requests as `<type>(#<issue>): <summary>`, for example
  `chore(#774): add local and CI PR guardrails`.
- Link exactly one issue in the PR body with a closing keyword such as
  `Closes #774`.

## Local Git Hooks

Install the repository hooks after cloning:

```powershell
.\scripts\setup-git-hooks.ps1
```

That configures `core.hooksPath` to `.githooks` and sets the local commit
template. The hooks block direct commits to `main`, enforce issue-based branch
naming, and validate Conventional Commit messages before Git accepts a commit.

## Local Validation

Before opening a pull request, run the CI-aligned validation commands:

```powershell
dotnet restore .\src\
dotnet build .\src\ --no-restore --configuration Release
dotnet test .\src\ --no-build --verbosity normal --configuration Release `
  --filter "FullyQualifiedName!~SyndicationFeedReader"
```

## Commit Message Format

```text
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### 1. Types

| Type         | Purpose                                    |
| :----------- | ------------------------------------------ |
| **feat**     | Introduce a new feature                    |
| **fix**      | Fix a bug                                  |
| **docs**     | Documentation changes only                 |
| **style**    | Code style changes (no logic impact)       |
| **refactor** | Code restructuring without behavior change |
| **perf**     | Performance improvements                   |
| **test**     | Add or update tests                        |
| **chore**    | Maintenance tasks (build, tooling, deps)   |

***

### 2. Scope (Optional)

Indicates the area affected. Example:

```text
feat(auth): add JWT authentication
```

***

### 3. Description

- Use **imperative mood** (e.g., "add" not "added").
- Keep it short (about 50 characters).

***

### 4. Body (Optional)

Explain **why** the change was made and any details for reviewers.

***

### 5. Footer (Optional)

- **BREAKING CHANGE:** Describe what changed and migration steps.
- Issue references (e.g., `Closes #123`).

Example:

```text
feat(api): add new endpoint for user profiles

BREAKING CHANGE: The old `/users` endpoint has been removed.
```

Pull request titles follow the same pattern, but the scope must be the linked
issue number:

```text
feat(#774): add local and CI PR guardrails
```

***

## Examples

- `feat: add user login functionality`
- `fix(auth): correct token expiration logic`
- `docs: update README with setup instructions`
- `chore(deps): bump axios to v1.2.0`

***

## Commit Template

You can copy this template for your commits:

```text
<type>(<scope>): <short summary>

[Optional body: Explain what and why]

[Optional footer: BREAKING CHANGE or issue refs]
```
