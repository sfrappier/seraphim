. "$PSScriptRoot\Common.ps1"
$editor = Get-UnityEditor
Start-Process -FilePath $editor -ArgumentList "-projectPath `"$ProjectRoot`""
