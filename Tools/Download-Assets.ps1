# Downloads creator-published CC0 assets and an OFL font; no authenticated Asset Store scraping.
param([switch]$Refresh)
. "$PSScriptRoot\Common.ps1"
$cache = Join-Path $ProjectRoot '.cache\assets'
$thirdParty = Join-Path $ProjectRoot 'Assets\ThirdParty\CC0'
New-Item -ItemType Directory -Force -Path $cache, $thirdParty | Out-Null
$receipts = @()
$assetLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'asset-lock.json') -Raw | ConvertFrom-Json

function Assert-AssetHash([string]$Path, [string]$Key) {
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $assetLock.$Key) { throw "Asset $Key differs from the pinned version. Review the upstream update and asset-lock.json before upgrading." }
}

function Install-KenneyPack([string]$Slug, [string]$Destination) {
    $sourcePage = "https://kenney.nl/assets/$Slug"
    $zipFile = Join-Path $cache "$Destination.zip"
    if ($Refresh -or !(Test-Path -LiteralPath $zipFile)) {
        $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        $page = Invoke-WebRequest -Uri $sourcePage -WebSession $session -UseBasicParsing
        $match = [regex]::Match($page.Content, "data-download-id='([^']+)'")
        if (!$match.Success) { throw "No public download was found on $sourcePage" }
        $link = Invoke-RestMethod -Uri ("https://kenney.nl/download-link/" + $match.Groups[1].Value) -WebSession $session -Headers @{
            Referer=$sourcePage; Origin='https://kenney.nl'; 'X-Requested-With'='XMLHttpRequest'
        }
        if (!$link.url.StartsWith('https://kenney.nl/')) { throw 'Unexpected asset download host.' }
        Invoke-WebRequest -Uri $link.url -WebSession $session -OutFile $zipFile
    }
    Assert-AssetHash $zipFile $Destination
    $extract = Join-Path $cache $Destination
    Expand-Archive -LiteralPath $zipFile -DestinationPath $extract -Force
    $output = Join-Path $thirdParty $Destination
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    # Retain FBX and their associated textures; other engine formats need not be imported.
    Get-ChildItem -LiteralPath $extract -Recurse -File | Where-Object {
        $_.Extension -in '.fbx','.png','.jpg','.jpeg','.txt','.md'
    } | ForEach-Object {
        $relative = $_.FullName.Substring($extract.Length).TrimStart('\','/')
        $target = Join-Path $output $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $target -Force
    }
    $script:receipts += [PSCustomObject]@{
        name=$Destination; source=$sourcePage; license='CC0-1.0'; archiveSha256=(Get-FileHash -LiteralPath $zipFile -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    Write-Host "Installed $Destination (CC0)."
}

Install-KenneyPack 'modular-dungeon-kit' 'KenneyDungeon'
Install-KenneyPack 'animated-characters-1' 'KenneyCharacters'

$stoneDirectory = Join-Path $thirdParty 'PolyHavenStone'
New-Item -ItemType Directory -Force -Path $stoneDirectory | Out-Null
$stoneInfo = Invoke-RestMethod -Uri 'https://api.polyhaven.com/files/stone_brick_wall_001' -Headers @{'User-Agent'='SeraphimPrototype/0.1'}
foreach ($map in @('Diffuse','nor_gl')) {
    $info = $stoneInfo.$map.'1k'.jpg
    if (!$info.url.StartsWith('https://dl.polyhaven.org/')) { throw 'Unexpected texture download host.' }
    $target = Join-Path $stoneDirectory ([IO.Path]::GetFileName(([uri]$info.url).AbsolutePath))
    if ($Refresh -or !(Test-Path -LiteralPath $target)) { Invoke-WebRequest -Uri $info.url -OutFile $target }
    if ((Get-FileHash -LiteralPath $target -Algorithm MD5).Hash.ToLowerInvariant() -ne $info.md5) { throw "Texture checksum mismatch: $target" }
    Assert-AssetHash $target ([IO.Path]::GetFileName($target))
}
$receipts += [PSCustomObject]@{name='PolyHavenStone';source='https://polyhaven.com/a/stone_brick_wall_001';license='CC0-1.0'}
$receipts | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $thirdParty 'asset-sources.json') -Encoding utf8
Write-Host 'Free prototype assets are ready.'

$fontDirectory=Join-Path $ProjectRoot 'Assets\ThirdParty\CinzelDecorative'
New-Item -ItemType Directory -Force -Path $fontDirectory | Out-Null
$fontRevision='3dd78844021e948ceb633d1dcee3f7885561b5d9'
foreach ($file in @('CinzelDecorative-Regular.ttf','OFL.txt')) {
    $target=Join-Path $fontDirectory $file
    if ($Refresh -or !(Test-Path -LiteralPath $target)) {
        Invoke-WebRequest -Uri ("https://raw.githubusercontent.com/google/fonts/"+$fontRevision+"/ofl/cinzeldecorative/"+$file) -OutFile $target
    }
    $key=if ($file -eq 'OFL.txt') { 'CinzelDecorative-OFL.txt' } else { $file }
    Assert-AssetHash $target $key
}
Write-Host 'Cinzel Decorative HUD font and OFL license verified.'

& "$PSScriptRoot\Download-CathedralAssets.ps1" -Refresh:$Refresh
