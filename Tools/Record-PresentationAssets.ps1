param()
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$snapshot=Join-Path $project '.cache/presentation/imported-files.json'
if (!(Test-Path -LiteralPath $snapshot)) { throw 'Original import inventory is missing; preserve it before adapting the packs.' }
$files=@(Get-Content -LiteralPath $snapshot -Raw | ConvertFrom-Json)
$packages=@(
    @{folder='Hovl Studio';title='Magic Effects FREE';publisher='Hovl Studio';version='1.6';productId=247933;source='https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933'},
    @{folder='SFX Packs';title='General RPG SFX Pack';publisher='Nickr02';version='1.0';productId=329076;source='https://assetstore.unity.com/packages/audio/sound-fx/general-rpg-sfx-pack-329076'},
    @{folder='Small Sound Kit';title='Small Sound Kit';publisher='Sound Works 12';version='1.0';productId=300582;source='https://assetstore.unity.com/packages/audio/sound-fx/small-sound-kit-300582'},
    @{folder='UnityTechnologies';title='Particle Pack | Starter Assets';publisher='Unity Technologies';version='3.3';productId=127325;source='https://assetstore.unity.com/packages/vfx/particles/particle-pack-starter-assets-127325'}
)
$records=@()
foreach ($package in $packages) {
    $entries=@($files | Where-Object { $_.file.StartsWith($package.folder+'/',[StringComparison]::Ordinal) } | Sort-Object file)
    if (!$entries.Count) { throw ('No original import receipt for '+$package.title) }
    $records+=[ordered]@{title=$package.title;publisher=$package.publisher;version=$package.version;productId=$package.productId;source=$package.source;license='Standard Unity Asset Store EULA';licenseUrl='https://unity.com/legal/as-terms';localRoot='Assets/ThirdParty/AssetStore/'+$package.folder;originalImportedFiles=$entries}
}
$manifest=[ordered]@{
    purpose='Optional local presentation packs for Seraphim 0.11.0. Raw and adapted assets remain in ignored AssetStore/. Public checkouts use original procedural presentation without these packs.'
    origin='Owner downloaded and imported licensed packages through Unity My Assets. Product IDs and versions were inspected in their AssetOrigin metadata; licenses checked against official listings.'
    adaptation='Preserve GUIDs when relocating imports; map 23 sound cues including four concrete footsteps; adapt 24 effect cues to the project URP particle shader; remove demo scripts, physics, embedded audio and lights from effect prefabs; recolor and bound particles. Never select blood/gore content.'
    setup='Import the listed versions under Assets/ThirdParty/AssetStore/ preserving the publisher subfolders, then run Tools/Build.ps1. DownloadedPresentationBuilder creates the private Resources bank. Bank, effects, materials and audio are excluded from Git.'
    originalInventory='Original imported files before local adaptations. These are metadata/checksum records, not redistributed asset content.'
    packages=$records
}
$manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'presentation-sources.json') -Encoding utf8
Write-Host ('Recorded four licensed packs and '+$files.Count+' imported-file checksums; no raw assets added to source control.')
