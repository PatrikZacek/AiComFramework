param([switch]$SkipReproducibility)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed ($LASTEXITCODE)." }
}
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $version = ([xml](Get-Content eng/Versions.props -Raw)).Project.PropertyGroup.Version
    $commit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'A Git checkout is required for Source Link.' }
    # Keep package cache paths below Windows' legacy MAX_PATH limit.
    $run = Join-Path $root "artifacts/v-$([guid]::NewGuid().ToString('N').Substring(0, 12))"
    $packages = Join-Path $run 'packages'
    New-Item -ItemType Directory -Path $packages -Force | Out-Null
    $buildProps = @('-p:ContinuousIntegrationBuild=true')
    Invoke-DotNet restore AIComFramework.slnx --locked-mode
    Invoke-DotNet build AIComFramework.slnx -c Release --no-restore @buildProps
    Invoke-DotNet test AIComFramework.slnx -c Release --no-build --no-restore --logger trx --results-directory (Join-Path $run 'test-results')
    Invoke-DotNet pack AIComFramework.slnx -c Release --no-build --no-restore --output $packages @buildProps
    & "$PSScriptRoot/Verify-Packages.ps1" -PackageDirectory $packages -Version $version -Commit $commit

    # Separate project with its own build imports, config, empty package cache and exact package versions.
    $consumer = Join-Path $run 'consumer'
    New-Item -ItemType Directory -Path $consumer | Out-Null
    Copy-Item "$PSScriptRoot/consumer/Program.cs" $consumer
    (Get-Content "$PSScriptRoot/consumer/Consumer.csproj" -Raw).Replace('__PACKAGE_VERSION__', $version) |
        Set-Content (Join-Path $consumer 'Consumer.csproj')
    '<Project />' | Set-Content (Join-Path $consumer 'Directory.Build.props')
    '<Project />' | Set-Content (Join-Path $consumer 'Directory.Build.targets')
    $feed = [System.Security.SecurityElement]::Escape($packages)
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="AIComFramework.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="Microsoft.*" /><package pattern="System.*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content (Join-Path $consumer 'NuGet.Config')
    $project = Join-Path $consumer 'Consumer.csproj'
    $consumerCache = Join-Path $root ".nuget/c-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
    New-Item -ItemType Directory -Path $consumerCache | Out-Null
    Invoke-DotNet restore $project --configfile (Join-Path $consumer 'NuGet.Config') --packages $consumerCache --no-cache
    Invoke-DotNet run --project $project -c Release --no-restore '--' $packages $commit
    $assets = Get-Content (Join-Path $consumer 'obj/project.assets.json') -Raw | ConvertFrom-Json
    if (@($assets.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'project' }).Count) {
        throw 'Consumer must use packages only, without ProjectReference.'
    }

    if (!$SkipReproducibility) {
        # Rebuild in a second checkout path to catch absolute paths and untracked source mistakes.
        $second = Join-Path $run 'repro-source'
        New-Item -ItemType Directory -Path $second | Out-Null
        # Copy exact working bytes, including pre-commit edits, without platform newline conversion.
        $changed = @(& git ls-files --cached --others --exclude-standard)
        if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed.' }
        foreach ($path in $changed) {
            $source = Join-Path $root $path
            $destination = Join-Path $second $path
            if (!(Test-Path -LiteralPath $source)) { throw 'Commit deleted files before running reproducibility verification.' }
            New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $source -Destination $destination -Force
        }
        # The source copy needs the same Git identity, but is not a worktree or another remote branch.
        & git -C $second init --quiet
        & git -C $second remote add origin https://github.com/PatrikZacek/AiComFramework.git
        & git -C $second fetch --quiet $root $commit
        if ($LASTEXITCODE -ne 0) { throw 'Cannot copy local Git identity for reproducibility check.' }
        & git -C $second reset --mixed --quiet $commit
        if ($LASTEXITCODE -ne 0) { throw 'Cannot set reproducibility checkout identity.' }
        $branchRef = & git symbolic-ref -q HEAD
        if ($branchRef) {
            & git -C $second update-ref $branchRef $commit
            if ($LASTEXITCODE -ne 0) { throw 'Cannot copy branch identity.' }
            & git -C $second symbolic-ref HEAD $branchRef
        } else {
            & git -C $second update-ref --no-deref HEAD $commit
        }
        if ($LASTEXITCODE -ne 0) { throw 'Cannot copy HEAD identity.' }
        $secondPackages = Join-Path $run 'repro-packages'
        Push-Location $second
        try {
            Invoke-DotNet restore AIComFramework.slnx --locked-mode
            Invoke-DotNet build AIComFramework.slnx -c Release --no-restore @buildProps
            Invoke-DotNet pack AIComFramework.slnx -c Release --no-build --no-restore --output $secondPackages @buildProps
        } finally { Pop-Location }
        foreach ($first in Get-ChildItem $packages -File) {
            # ZIP container timestamps/IDs need not be reproducible; every payload entry must be.
            $left = [IO.Compression.ZipFile]::OpenRead($first.FullName)
            $right = [IO.Compression.ZipFile]::OpenRead((Join-Path $secondPackages $first.Name))
            try {
                $corePattern = '^package/services/metadata/core-properties/[^/]+\.psmdcp$'
                $leftNames = @($left.Entries.FullName | ForEach-Object { $_ -replace $corePattern, 'core.psmdcp' } | Sort-Object)
                $rightNames = @($right.Entries.FullName | ForEach-Object { $_ -replace $corePattern, 'core.psmdcp' } | Sort-Object)
                if (@(Compare-Object $leftNames $rightNames).Count) { throw "Reproducibility entry mismatch: $($first.Name)" }
                foreach ($entry in $left.Entries) {
                    $other = if ($entry.FullName -match $corePattern) {
                        $right.Entries | Where-Object FullName -Match $corePattern
                    } else { $right.GetEntry($entry.FullName) }
                    $a = $entry.Open(); $b = $other.Open()
                    try {
                        if ($entry.FullName -ceq '_rels/.rels') {
                            $readerA = [IO.StreamReader]::new($a); $readerB = [IO.StreamReader]::new($b)
                            try {
                                $relA = $readerA.ReadToEnd() -replace ' Id="[^"]+"', '' -replace 'core-properties/[^/]+\.psmdcp', 'core-properties/core.psmdcp'
                                $relB = $readerB.ReadToEnd() -replace ' Id="[^"]+"', '' -replace 'core-properties/[^/]+\.psmdcp', 'core-properties/core.psmdcp'
                                if ($relA -cne $relB) { throw 'Non-reproducible package relationships.' }
                            } finally { $readerA.Dispose(); $readerB.Dispose() }
                            continue
                        }
                        $sha = [Security.Cryptography.SHA256]::Create()
                        try {
                            $hashA = [Convert]::ToHexString($sha.ComputeHash($a))
                            $hashB = [Convert]::ToHexString($sha.ComputeHash($b))
                        } finally { $sha.Dispose() }
                        if ($hashA -cne $hashB) { throw "Non-reproducible payload: $($first.Name)/$($entry.FullName)" }
                    } finally { $a.Dispose(); $b.Dispose() }
                }
            } finally { $left.Dispose(); $right.Dispose() }
        }
        Write-Host 'Release package payloads are reproducible across two checkout paths.'
    }
    $hashes = Get-ChildItem $packages -File | Sort-Object Name | ForEach-Object {
        "$((Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $($_.Name)"
    }
    $hashes | Set-Content (Join-Path $packages 'SHA256SUMS')
    if ($env:GITHUB_OUTPUT) {
        "packages=$packages" | Add-Content $env:GITHUB_OUTPUT
        "results=$(Join-Path $run 'test-results')" | Add-Content $env:GITHUB_OUTPUT
        "manifest=$(Join-Path $packages 'SHA256SUMS')" | Add-Content $env:GITHUB_OUTPUT
        "version=$version" | Add-Content $env:GITHUB_OUTPUT
    }
    Write-Host "Verified packages: $packages"
} finally { Pop-Location }
