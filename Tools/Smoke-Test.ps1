param([switch]$CombatOnly,[switch]$ScenarioOnly,[switch]$ControllerOnly,[switch]$DifficultyOnly,[switch]$FrontEndOnly,[switch]$FallbackMusic)
. "$PSScriptRoot\Common.ps1"
$player = Join-Path $ProjectRoot 'Builds\Windows\Seraphim.exe'
if (!(Test-Path -LiteralPath $player)) { throw 'Build the Windows player first with Tools/Build.ps1.' }
$results = Join-Path $ProjectRoot 'Builds\Windows\SmokeTests\results.json'
# Rename a previous report so an interrupted run cannot be mistaken for success.
if (Test-Path -LiteralPath $results) { Move-Item -LiteralPath $results -Destination ($results+'.previous') -Force }
$log = Join-Path $ProjectRoot 'Logs\player-smoke.log'
if ((@($CombatOnly,$ScenarioOnly,$ControllerOnly,$DifficultyOnly,$FrontEndOnly) | Where-Object { $_ }).Count -gt 1) { throw 'Choose one focused test mode.' }
$extra=if ($CombatOnly) { ' -seraphim-combat-only' } elseif ($ScenarioOnly) { ' -seraphim-scenario-only' } elseif ($ControllerOnly) { ' -seraphim-controller-only' } elseif ($DifficultyOnly) { ' -seraphim-difficulty-only' } elseif ($FrontEndOnly) { ' -seraphim-front-end-only' } else { '' }
if ($FallbackMusic) { $extra+=' -seraphim-fallback-music' }
$process = Start-Process -FilePath $player -ArgumentList "-screen-width 1600 -screen-height 900 -logFile `"$log`" -seraphim-smoke$extra" -WindowStyle Hidden -PassThru
$watch=[Diagnostics.Stopwatch]::StartNew()
$nextProgress=15
while (!$process.WaitForExit(1000)) {
    if ($watch.Elapsed.TotalSeconds -gt 180) {
        Stop-Process -Id $process.Id
        throw "Player smoke test timed out. See $log."
    }
    if ($watch.Elapsed.TotalSeconds -ge $nextProgress) {
        Write-Host "Player checks running ($nextProgress seconds; includes the ultimate recharge test)..."
        $nextProgress+=15
    }
}
if (!(Test-Path -LiteralPath $results)) { Get-Content -LiteralPath $log -Tail 40; throw 'Player did not produce a smoke-test report.' }
$report = Get-Content -LiteralPath $results -Raw | ConvertFrom-Json
$report | ConvertTo-Json -Depth 4
if (!$report.success -or $process.ExitCode -ne 0) { throw "Player smoke test failed. See $log." }
