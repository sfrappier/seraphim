# Inventory an owner-downloaded original archive before Unity import/conversion.
param(
    [Parameter(Mandatory=$true)][ValidateSet('fallen-angel','cinder-reaver','ash-invoker','obsidian-brute','rift-scourger','infernal-wyrm')][string]$Model,
    [Parameter(Mandatory=$true)][string]$Archive
)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'character-sources.json') -Raw | ConvertFrom-Json
$definition=$manifest.models | Where-Object key -eq $Model
$archivePath=(Resolve-Path -LiteralPath $Archive).Path
if ([IO.Path]::GetExtension($archivePath) -ne '.zip') { throw 'Supply the original ZIP download; leave embedded model files intact.' }
$digest=(Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$stageBase=[IO.Path]::GetFullPath((Join-Path $project '.cache\model-staging'))
$stage=[IO.Path]::GetFullPath((Join-Path $stageBase ($Model+'-'+$digest.Substring(0,16))))
if (!$stage.StartsWith($stageBase+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid staging directory.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $expanded=0L
    # Validate every name before extracting anything; reject absolute paths and ADS.
    foreach ($entry in $zip.Entries) {
        $relative=$entry.FullName.Replace('/',[IO.Path]::DirectorySeparatorChar)
        $isFolder=$entry.FullName.EndsWith('/') -or $entry.FullName.EndsWith('\')
        if ([IO.Path]::IsPathRooted($relative) -or $relative.Contains(':')) { throw "Unsafe archive entry: $($entry.FullName)" }
        $target=[IO.Path]::GetFullPath((Join-Path $stage $relative))
        if (!($isFolder -and $target.TrimEnd('\') -eq $stage) -and !$target.StartsWith($stage+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe archive entry: $($entry.FullName)" }
        $expanded+=$entry.Length
        if ($expanded -gt 8GB) { throw 'Archive exceeds the 8 GB staging limit.' }
    }
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    foreach ($entry in $zip.Entries) {
        $relative=$entry.FullName.Replace('/',[IO.Path]::DirectorySeparatorChar)
        $target=[IO.Path]::GetFullPath((Join-Path $stage $relative))
        if ($entry.FullName.EndsWith('/') -or $entry.FullName.EndsWith('\')) { New-Item -ItemType Directory -Path $target -Force | Out-Null; continue }
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$target,$true)
    }
}
finally { $zip.Dispose() }
$files=@(Get-ChildItem -LiteralPath $stage -Recurse -File | Where-Object Name -ne 'staging-receipt.json' | Sort-Object FullName | ForEach-Object {
    [ordered]@{file=$_.FullName.Substring($stage.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$receipt=[ordered]@{
    status='Staged for inspection; not installed or validated in Unity.'
    model=$definition.key; title=$definition.title; creator=$definition.creator; source=$definition.source
    sourceUid=$definition.uid; license=$manifest.license.id; licenseUrl=$manifest.license.url
    archiveFile=[IO.Path]::GetFileName($archivePath); archiveSha256=$digest
    files=$files
}
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $stage 'staging-receipt.json') -Encoding utf8
Write-Host "Staged $Model in $stage"
Write-Host "Archive SHA256: $digest"
$files | ForEach-Object { [pscustomobject]$_ } | Where-Object { [IO.Path]::GetExtension($_.file) -in '.fbx','.blend','.gltf','.glb','.zip' } | Select-Object file,bytes | Format-Table -AutoSize
