. "$PSScriptRoot\Common.ps1"
$player = Join-Path $ProjectRoot 'Builds\Windows\Seraphim.exe'
if (!(Test-Path -LiteralPath $player)) { throw 'Build the Windows player first with Tools/Build.ps1.' }
$results = Join-Path $ProjectRoot 'Builds\Windows\SmokeTests\results.json'
# Rename a previous report so an interrupted run cannot be mistaken for success.
if (Test-Path -LiteralPath $results) { Move-Item -LiteralPath $results -Destination ($results+'.previous') -Force }
$log = Join-Path $ProjectRoot 'Logs\player-smoke.log'
$process = Start-Process -FilePath $player -ArgumentList "-screen-width 1600 -screen-height 900 -logFile `"$log`" -seraphim-smoke" -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(60000)) {
    Stop-Process -Id $process.Id
    throw "Player smoke test timed out. See $log."
}
if (!(Test-Path -LiteralPath $results)) { Get-Content -LiteralPath $log -Tail 40; throw 'Player did not produce a smoke-test report.' }
$report = Get-Content -LiteralPath $results -Raw | ConvertFrom-Json
$report | ConvertTo-Json -Depth 4
if (!$report.success -or $process.ExitCode -ne 0) { throw "Player smoke test failed. See $log." }
