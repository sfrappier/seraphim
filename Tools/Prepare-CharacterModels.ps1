# Optional OFFLINE source preparation. Ordinary Tools/Build.ps1 uses committed
# derivatives and requires neither Blender nor a Sketchfab account/download.
param([switch]$RecordOnly)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$definitions=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'character-sources.json') -Raw | ConvertFrom-Json
$cache=Join-Path $project '.cache'
$toolsCache=Join-Path $cache 'model-tools'
$assetRoot=Join-Path $project 'Assets/ThirdParty/CCBY/Characters'
$recipes=@{
    'fallen-angel'=@('fallenangel-free-game-character-ue5-rig.zip','source/FallenAngel.fbx')
    'cinder-reaver'=@('realistic-devil-demon-3d-model-game-ready.zip','source/devil.zip')
    'ash-invoker'=@('demon-witch-stylized-game-ready-character.zip','source/WitchRIG_NEWRIG.fbx')
    'obsidian-brute'=@('minotaurs.zip','source/Minotaurs.fbx')
    'rift-scourger'=@('scourger-rig.zip','source/Scourger3.blend')
    'infernal-wyrm'=@('dragon-nemesis-the-infernal-wyrm.zip','source/DRAGON_NEMESIS.glb')
}
function Stage-Path([string]$key,[string]$archive) {
    $sha=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    Join-Path $cache ('model-staging/'+$key+'-'+$sha.Substring(0,16))
}
$inputs=@()
foreach ($definition in $definitions.models) {
    $recipe=$recipes[$definition.key]
    $archive=Join-Path $cache ('model-downloads/'+$recipe[0])
    if (!(Test-Path -LiteralPath $archive)) { throw "Missing original archive: $archive. Download it while signed into the source linked in Tools/character-sources.json." }
    $stage=Stage-Path $definition.key $archive
    if (!$RecordOnly) { & (Join-Path $PSScriptRoot 'Stage-CharacterModel.ps1') -Model $definition.key -Archive $archive }
    $source=Join-Path $stage $recipe[1]
    $receipt=Join-Path $stage 'staging-receipt.json'
    if ($definition.key -eq 'cinder-reaver') {
        $nestedStage=Stage-Path $definition.key $source
        if (!$RecordOnly) { & (Join-Path $PSScriptRoot 'Stage-CharacterModel.ps1') -Model $definition.key -Archive $source }
        $source=Join-Path $nestedStage 'devil/devil.fbx'
    }
    $inputs+=@{key=$definition.key;source=$source;receipt=$receipt}
}
if (!$RecordOnly) {
    New-Item -ItemType Directory -Path $toolsCache -Force | Out-Null
    $version='4.5.9'
    $url="https://download.blender.org/release/Blender4.5/blender-$version-windows-x64.zip"
    $checksum='41da973b9bf95bb312cbeff4d1982feb13259b43c821686b9bafea4dfe5477cf'
    $archive=Join-Path $toolsCache "blender-$version-windows-x64.zip"
    if (!(Test-Path -LiteralPath $archive)) { Invoke-WebRequest -Uri $url -OutFile $archive }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $checksum) { throw 'Pinned Blender archive checksum mismatch.' }
    $executable=Join-Path $toolsCache "blender-$version-windows-x64/blender.exe"
    if (!(Test-Path -LiteralPath $executable)) { Expand-Archive -LiteralPath $archive -DestinationPath $toolsCache -Force }
    $inputs | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $cache 'character-inputs.json') -Encoding utf8
    $python=Join-Path $PSScriptRoot 'Prepare-CharacterModels.py'
    & $executable --background --factory-startup --disable-autoexec --python-exit-code 1 --python $python
    if ($LASTEXITCODE -ne 0) { throw 'Character derivative preparation failed.' }
}
$license=Join-Path $assetRoot 'CC-BY-4.0.txt'
if (!(Test-Path -LiteralPath $license)) { Invoke-WebRequest -Uri $definitions.license.textUrl -OutFile $license }
if ((Get-FileHash -LiteralPath $license -Algorithm SHA256).Hash.ToLowerInvariant() -ne $definitions.license.textSha256) { throw 'CC BY 4.0 text checksum mismatch.' }
$tool=[ordered]@{name='Blender';version='4.5.9 LTS';license='GPL-3.0-or-later';source='https://download.blender.org/release/Blender4.5/blender-4.5.9-windows-x64.zip';sha256='41da973b9bf95bb312cbeff4d1982feb13259b43c821686b9bafea4dfe5477cf';requiredForBuild=$false}
$credits=@('SERAPHIM CHARACTER CREDITS','','All models below: Creative Commons Attribution 4.0 International.','License: https://creativecommons.org/licenses/by/4.0/','Full terms: CC-BY-4.0.txt','','Adapted for Seraphim: Unity FBX conversion, game skeletons where absent,','skeletal movement, PBR channel conversion, texture resizing, mesh LODs,','normalization, celestial crown and six procedural wings for FallenAngel.','No endorsement by the original authors is implied.','')
foreach ($input in $inputs) {
    $definition=$definitions.models | Where-Object key -eq $input.key
    $receipt=Get-Content -LiteralPath $input.receipt -Raw | ConvertFrom-Json
    $folder=Join-Path $assetRoot $input.key
    $files=@(Get-ChildItem -LiteralPath $folder -File | Where-Object { $_.Extension -ne '.meta' -and $_.Name -ne 'source.json' } | Sort-Object Name | ForEach-Object { [ordered]@{file=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()} })
    if (!$files.Count) { throw "Runtime derivatives missing: $folder" }
    $provenance=[ordered]@{
        title=$definition.title;creator=$definition.creator;source=$definition.source;uid=$definition.uid
        license=$definitions.license.id;licenseUrl=$definitions.license.url
        originalArchive=$receipt.archiveFile;originalArchiveSha256=$receipt.archiveSha256;originalFiles=$receipt.files
        preparationTool=$tool
        modifications='Converted to FBX/Unity PBR; resized textures, packed metallic/smoothness; retained author skeletons where present; supplied game rigs for Devil Demon, Demon Witch and Infernal Wyrm; two demon/boss mesh LODs. Original unmodified downloads remain outside the source tree.'
        runtimeFiles=$files
    }
    if ($input.key -eq 'cinder-reaver') {
        $nested=Get-Content -LiteralPath (Join-Path (Split-Path (Split-Path $input.source -Parent) -Parent) 'staging-receipt.json') -Raw | ConvertFrom-Json
        $provenance.nestedArchiveSha256=$nested.archiveSha256
        $provenance.nestedOriginalFiles=$nested.files
    }
    if ($input.key -eq 'infernal-wyrm') {
        $provenance.modifications+=' Refined anatomical skin weights with continuous joint blends; separated hand-bound rigid sword, a blade-tip joint and a cape joint for authored skeletal locomotion, cast and attack poses.'
    }
    $provenance | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $folder 'source.json') -Encoding utf8
    $credits+=@($definition.title,('Creator: '+$definition.creator),('Source: '+$definition.source),'License: CC BY 4.0','')
}
$credits | Set-Content -LiteralPath (Join-Path $assetRoot 'CREDITS.txt') -Encoding utf8
Write-Host 'Six character derivatives and source/checksum/attribution records prepared.'
