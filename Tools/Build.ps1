param([switch]$Run)
. "$PSScriptRoot\Common.ps1"
Invoke-UnityTask 'Seraphim.Editor.ProjectBuilder.BuildWindows' 'build-windows.log'
if ($Run) { Start-Process -FilePath (Join-Path $ProjectRoot 'Builds\Windows\Seraphim.exe') }
