param(
    [string] $Remote = "origin",
    [string] $Branch = "main",
    [string] $TagPrefix = "v",
    [string] $FirstVersion = "4.0.0",
    [switch] $DryRun
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return $output
}

$firstVersionMatch = [regex]::Match($FirstVersion, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$')
if (-not $firstVersionMatch.Success) {
    throw "First version '$FirstVersion' must use MAJOR.MINOR.PATCH format."
}

Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)
$repoRoot = (Invoke-Git -Arguments @("rev-parse", "--show-toplevel")).Trim()
Set-Location $repoRoot

if (Invoke-Git -Arguments @("status", "--porcelain")) {
    throw "Working tree is not clean. Commit or stash changes before releasing."
}

Write-Host "Fetching '$Remote'..."
Invoke-Git -Arguments @("fetch", "--prune", "--tags", $Remote) | Out-Null

$remoteBranchRef = "refs/remotes/$Remote/$Branch"
& git show-ref --verify --quiet $remoteBranchRef
if ($LASTEXITCODE -ne 0) {
    throw "Remote branch '$Remote/$Branch' does not exist."
}

$localBranchRef = "refs/heads/$Branch"
& git show-ref --verify --quiet $localBranchRef
if ($LASTEXITCODE -eq 0) {
    Invoke-Git -Arguments @("switch", $Branch) | Out-Null
}
elseif ($LASTEXITCODE -eq 1) {
    Invoke-Git -Arguments @("switch", "--track", "-c", $Branch, "$Remote/$Branch") | Out-Null
}
else {
    throw "Unable to inspect local branch '$Branch'."
}

Invoke-Git -Arguments @("pull", "--ff-only", $Remote, $Branch) | Out-Null

$escapedPrefix = [regex]::Escape($TagPrefix)
$tagPattern = "^$escapedPrefix(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$"

$latest = Invoke-Git -Arguments @("tag", "--list", "$TagPrefix*") |
    ForEach-Object {
        $tag = $_.Trim()
        if ($tag -match $tagPattern) {
            [pscustomobject]@{
                Tag   = $tag
                Major = [int] $Matches["major"]
                Minor = [int] $Matches["minor"]
                Patch = [int] $Matches["patch"]
            }
        }
    } |
    Sort-Object Major, Minor, Patch |
    Select-Object -Last 1

if ($latest) {
    $next = "$($latest.Major).$($latest.Minor).$($latest.Patch + 1)"
    $previousTag = $latest.Tag
}
else {
    $next = $FirstVersion
    $previousTag = "<none>"
}

$nextTag = "$TagPrefix$next"
if (Invoke-Git -Arguments @("tag", "--list", $nextTag)) {
    throw "Tag '$nextTag' already exists."
}

Write-Host "Previous version tag: $previousTag"
Write-Host "Next version tag:     $nextTag"

if ($DryRun) {
    Write-Host "Dry run: no tag was created or pushed."
    return
}

Invoke-Git -Arguments @("tag", "-a", $nextTag, "-m", "Release $nextTag") | Out-Null
Invoke-Git -Arguments @("push", $Remote, $Branch, $nextTag) | Out-Null

Write-Host "Pushed '$nextTag'. GitHub Actions will publish both NuGet packages."
