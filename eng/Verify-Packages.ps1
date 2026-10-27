param(
    [Parameter(Mandatory)][string]$PackageDirectory,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Commit
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression
$ids = @('AIComFramework.Abstractions', 'AIComFramework.Core', 'AIComFramework.Serialization.Json')
$files = @(Get-ChildItem -LiteralPath $PackageDirectory -File)
$expected = @($ids | ForEach-Object { "$_.${Version}.nupkg"; "$_.${Version}.snupkg" })
if (@(Compare-Object ($files.Name | Sort-Object) ($expected | Sort-Object)).Count) {
    throw 'Expected exactly three NuGet packages and three symbol packages.'
}
foreach ($id in $ids) {
    $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $PackageDirectory "$id.$Version.nupkg"))
    try {
        $names = @($archive.Entries.FullName)
        foreach ($required in @('README.md', 'LICENSE', "lib/net10.0/$id.dll", "lib/net10.0/$id.xml")) {
            if ($required -cnotin $names) { throw "Missing $required in $id" }
        }
        foreach ($name in $names) {
            if ($name -match '(?i)(samples|tests|\.cs$|\.pdb$)') { throw "Unexpected package entry $name" }
            if ($name -like '*.dll' -and $name -cne "lib/net10.0/$id.dll") { throw "Bundled dependency: $name" }
        }
        $nuspec = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
        if ($nuspec.Count -ne 1) { throw "Expected one nuspec in $id" }
        $reader = [IO.StreamReader]::new($nuspec[0].Open())
        try { [xml]$xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $meta = $xml.package.metadata
        if ($meta.id -cne $id -or $meta.version -cne $Version) { throw "Incorrect identity/version in $id" }
        if ($meta.license.type -ne 'expression' -or $meta.license.InnerText -ne 'MIT') { throw "Incorrect license in $id" }
        if ($meta.readme -ne 'README.md' -or !$meta.description -or !$meta.authors -or !$meta.tags) { throw "Incomplete metadata in $id" }
        if ($meta.repository.url -ne 'https://github.com/PatrikZacek/AiComFramework' -or
            $meta.repository.commit -cne $Commit -or $meta.repository.type -ne 'git') { throw "Incorrect repository metadata in $id" }
        $deps = @($meta.SelectNodes("*[local-name()='dependencies']/*[local-name()='group']/*[local-name()='dependency']"))
        $expectedDeps = switch ($id) {
            'AIComFramework.Abstractions' { @() }
            'AIComFramework.Core' { @('AIComFramework.Abstractions', 'Microsoft.Extensions.AI', 'Microsoft.Extensions.DependencyInjection.Abstractions') }
            'AIComFramework.Serialization.Json' { @('AIComFramework.Abstractions', 'Microsoft.Extensions.DependencyInjection.Abstractions') }
        }
        $depIds = @($deps | ForEach-Object { $_.id })
        if (@(Compare-Object (@('sentinel') + @($depIds | Sort-Object)) (@('sentinel') + @($expectedDeps | Sort-Object))).Count) { throw "Incorrect dependency graph in $id" }
        foreach ($dep in $deps) {
            if ($dep.id -eq 'AIComFramework.Abstractions' -and $dep.version -ne $Version) { throw "Incorrect sibling version in $id" }
        }
    } finally { $archive.Dispose() }
    $symbols = [IO.Compression.ZipFile]::OpenRead((Join-Path $PackageDirectory "$id.$Version.snupkg"))
    try {
        if (@($symbols.Entries | Where-Object FullName -CEQ "lib/net10.0/$id.pdb").Count -ne 1) { throw "Missing portable PDB for $id" }
    } finally { $symbols.Dispose() }
}
Write-Host 'Package identities, metadata, contents, dependency graph and symbols passed.'
