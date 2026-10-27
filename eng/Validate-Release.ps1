param([Parameter(Mandatory)][string]$Tag)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$version = ([xml](Get-Content "$PSScriptRoot/Versions.props" -Raw)).Project.PropertyGroup.Version
$numeric = '(0|[1-9][0-9]*)'
$identifier = '(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)'
if ($Tag -cnotmatch "^v$numeric\.$numeric\.$numeric(-$identifier(\.$identifier)*)?$" -or $Tag -cne "v$version") {
    throw "Tag must be a canonical v<SemVer> matching eng/Versions.props ($version)."
}
& git fetch origin main --no-tags
if ($LASTEXITCODE -ne 0) { throw 'Cannot fetch main.' }
& git merge-base --is-ancestor HEAD origin/main
if ($LASTEXITCODE -ne 0) { throw 'Release commit must belong to main.' }
& git diff --exit-code
if ($LASTEXITCODE -ne 0) { throw 'Release checkout must be clean.' }
if (!(Test-Path "$PSScriptRoot/../LICENSE")) { throw 'Release requires the owner-approved license.' }
Write-Host "Release tag $Tag is valid and belongs to main."
