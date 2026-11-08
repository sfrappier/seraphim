param(
    [Parameter(Mandatory=$true)][string]$InputFile,
    [string]$FfmpegPath = (Join-Path $PSScriptRoot '..\.cache\audio-tools\ffmpeg.exe'),
    [switch]$AlreadyTrimmed
)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'music-source.json') -Raw | ConvertFrom-Json
if (!(Test-Path -LiteralPath $InputFile)) { throw 'Supply a local authorized recording or the selected downloaded segment.' }
if (!(Test-Path -LiteralPath $FfmpegPath)) { throw 'Supply FfmpegPath to an installed ffmpeg executable.' }
$localFolder=Join-Path $project 'LocalAudio'
New-Item -ItemType Directory -Path $localFolder -Force | Out-Null
$destination=Join-Path $localFolder 'seraphim-theme.ogg'
if ([IO.Path]::GetFullPath($InputFile) -eq [IO.Path]::GetFullPath($destination)) { throw 'Input must differ from output.' }
$arguments=@('-hide_banner','-loglevel','error','-y')
if (!$AlreadyTrimmed) { $arguments+=@('-ss',[string]$source.startSeconds) }
$arguments+=@('-i',$InputFile,'-t',[string]$source.durationSeconds,'-vn','-af','afade=t=in:st=0:d=0.5,afade=t=out:st=1908.5:d=0.5','-c:a','libvorbis','-q:a','5',$destination)
& $FfmpegPath @arguments
if ($LASTEXITCODE -ne 0) { throw 'Music conversion failed.' }
$receipt=[ordered]@{
    source=$source.source; creator=$source.creator; section=$source.section
    startSeconds=$source.startSeconds; endSeconds=$source.endSeconds; durationSeconds=$source.durationSeconds
    permission=$source.permission; processing=$source.processing
    inputSha256=(Get-FileHash -LiteralPath $InputFile -Algorithm SHA256).Hash.ToLowerInvariant()
    sha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
}
$receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $localFolder 'source.json') -Encoding utf8
Write-Host "Imported local authorized music: $destination"
Write-Host "SHA256: $($receipt.sha256)"
