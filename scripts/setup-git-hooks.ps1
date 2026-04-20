[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = git rev-parse --show-toplevel 2>$null

if (-not $repoRoot) {
    throw 'Run this script from inside the jjgnet-broadcast repository.'
}

Push-Location $repoRoot
try {
    git config --local core.hooksPath .githooks
    git config --local commit.template .squad/commit-msg.txt

    Write-Host 'Configured local Git hooks for jjgnet-broadcast.'
    Write-Host '  core.hooksPath = .githooks'
    Write-Host '  commit.template = .squad/commit-msg.txt'
    Write-Host ''
    Write-Host 'Git will now block commits to main, enforce issue-based branch names, and validate Conventional Commit messages.'
}
finally {
    Pop-Location
}
