# GitHub PR Comment via gh api (Windows PowerShell)

## When to use
- You need to leave a visible PR/issue comment from the CLI.
- A regular comment is required instead of a formal GitHub review.

## Pattern
1. Build the comment body in PowerShell.
2. Serialize `{ body = <comment text> }` to JSON with `ConvertTo-Json`.
3. Write that JSON to a file inside the repo (not `/tmp`).
4. Post with `gh api repos/<owner>/<repo>/issues/<number>/comments --method POST --input <json-file>`.
5. Delete the temporary JSON file after the request succeeds.

## Content guidelines — GFM required

Before composing the comment body:
- Use **GitHub Flavored Markdown (GFM)** — inline code uses backticks: `` `path/to/file.cs` ``, `` `MyMethod()` ``
- **NEVER** write `\text\` (backslash-word-backslash) — it renders as literal backslashes on GitHub, not code
- **NEVER** write `\\\` as a code fence — use triple backticks (` ``` `) instead
- **Self-check:** before posting, scan the body for `\word\` patterns and replace with `` `word` ``

## Example
```powershell
$payloadPath = '.\gh-comment-123.json'
@{ body = 'Neo review note: use ``MyMethod()`` not ``\MyMethod\``.' } |
  ConvertTo-Json -Compress |
  Set-Content -Path $payloadPath -Encoding utf8

gh api repos/owner/repo/issues/123/comments --method POST --input $payloadPath

Remove-Item $payloadPath -Force
```

## Why
- Avoids shell-escaping issues with multiline comment bodies on Windows.
- Produces a normal visible PR comment, which matches squad protocol for author-owned PRs.
