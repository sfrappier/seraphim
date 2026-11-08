# Pinned CC0 dependencies for the cathedral milestone. Receipts are source files.
param([switch]$Refresh)
. "$PSScriptRoot\Common.ps1"
$ErrorActionPreference='Stop'
function Get-PinnedFile([string]$Url,[string]$Path,[string]$Sha256) {
    if ($Refresh -or !(Test-Path -LiteralPath $Path)) { Invoke-WebRequest -Uri $Url -OutFile $Path }
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Sha256) {
        throw "Dependency checksum mismatch: $Path"
    }
}
$angelRoot=Join-Path $ProjectRoot 'Assets\ThirdParty\CC0\KayKitAngel'
$angel=Get-Content -LiteralPath (Join-Path $angelRoot 'source.json') -Raw | ConvertFrom-Json
$base='https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0/'+$angel.revision+'/'
foreach ($entry in $angel.files) {
    $relative=switch ($entry.file) {
        'Knight.fbx' { 'addons/kaykit_character_pack_adventures/Characters/fbx/Knight.fbx' }
        'knight_texture.png' { 'addons/kaykit_character_pack_adventures/Assets/fbx/knight_texture.png' }
        'LICENSE.txt' { 'LICENSE.txt' }
        default { throw 'Unexpected character dependency.' }
    }
    Get-PinnedFile ($base+$relative) (Join-Path $angelRoot $entry.file) $entry.sha256
}
$textureRoot=Join-Path $ProjectRoot 'Assets\ThirdParty\CC0\CathedralTextures'
$textures=Get-Content -LiteralPath (Join-Path $textureRoot 'source.json') -Raw | ConvertFrom-Json
foreach ($entry in $textures) {
    if (!$entry.url.StartsWith('https://dl.polyhaven.org/')) { throw 'Unexpected texture host.' }
    Get-PinnedFile $entry.url (Join-Path $textureRoot $entry.file) $entry.sha256
}
Write-Host 'KayKit angel base and six 2K cathedral texture maps verified.'
