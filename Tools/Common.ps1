$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$EditorVersion = '6000.3.28f1'

function Get-UnityEditor {
    $candidates = @()
    if ($env:SERAPHIM_UNITY_EDITOR) { $candidates += $env:SERAPHIM_UNITY_EDITOR }
    $localConfig = Join-Path $PSScriptRoot 'local.json'
    if (Test-Path -LiteralPath $localConfig) {
        $candidates += (Get-Content -LiteralPath $localConfig -Raw | ConvertFrom-Json).unityEditor
    }
    $candidates += "C:\Program Files\Unity\Hub\Editor\$EditorVersion\Editor\Unity.exe"
    $candidates += "F:\Program Files\Unity\Hub\Editor\$EditorVersion\Editor\Unity.exe"
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $candidate }
    }
    throw "Unity $EditorVersion was not found. Install it in Unity Hub, or set SERAPHIM_UNITY_EDITOR to Unity.exe."
}

function Invoke-UnityTask([string]$Method, [string]$LogName) {
    # MSIX Hub keeps Personal activation in its package-local cache. The standalone
    # Editor uses the ordinary per-user license folder. Copy only the already
    # activated, signed license files for this same user/machine, without editing.
    $packageLicenses = Join-Path $env:LOCALAPPDATA 'Packages\UnityTechnologies.UnityHub_2vrhnee42bhxm\LocalCache\Local\Unity\licenses'
    $editorLicenses = Join-Path $env:LOCALAPPDATA 'Unity\licenses'
    if (Test-Path -LiteralPath $packageLicenses) {
        Get-ChildItem -LiteralPath $packageLicenses -Recurse -File -Filter '*.xml' | ForEach-Object {
            $relative = $_.FullName.Substring($packageLicenses.Length).TrimStart('\')
            $target = Join-Path $editorLicenses $relative
            if (!(Test-Path -LiteralPath $target) -or $_.LastWriteTimeUtc -gt (Get-Item -LiteralPath $target).LastWriteTimeUtc) {
                New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
                Copy-Item -LiteralPath $_.FullName -Destination $target -Force
            }
        }
    }
    $editor = Get-UnityEditor
    $logFolder = Join-Path $ProjectRoot 'Logs'
    New-Item -ItemType Directory -Force -Path $logFolder | Out-Null
    $logFile = Join-Path $logFolder $LogName
    Write-Host "Unity task: $Method"
    Write-Host "Log: $logFile"
    $arguments = "-batchmode -nographics -quit -projectPath `"$ProjectRoot`" -buildTarget StandaloneWindows64 -executeMethod $Method -logFile `"$logFile`""
    $task = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($task.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $logFile) { Get-Content -LiteralPath $logFile -Tail 65 }
        if ($task.ExitCode -eq 198) { throw 'Unity needs an active license. Sign in to Unity Hub and activate Unity Personal, then retry.' }
        throw "Unity failed with exit code $($task.ExitCode). See $logFile."
    }
    Write-Host 'Completed successfully.'
}
