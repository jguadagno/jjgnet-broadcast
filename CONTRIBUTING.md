# Contributing Guidelines

Thank you for contributing! This project follows the Conventional Commits
specification to keep the commit history clean and meaningful.

## Commit Message Format

    <type>[optional scope]: <description>

    [optional body]

    [optional footer(s)]

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

    feat(auth): add JWT authentication

***

### 3. Description

- Use **imperative mood** (e.g., "add" not "added").
- Keep it short (? 50 characters).

***

### 4. Body (Optional)

Explain **why** the change was made and any details for reviewers.

***

### 5. Footer (Optional)

- **BREAKING CHANGE:** Describe what changed and migration steps.
- Issue references (e.g., `Closes #123`).

Example:

    feat(api): add new endpoint for user profiles

    BREAKING CHANGE: The old `/users` endpoint has been removed.

***

## Examples

- `feat: add user login functionality`
- `fix(auth): correct token expiration logic`
- `docs: update README with setup instructions`
- `chore(deps): bump axios to v1.2.0`

***

## Commit Template

You can copy this template for your commits:

    <type>(<scope>): <short summary>

    [Optional body: Explain what and why]

    [Optional footer: BREAKING CHANGE or issue refs]
