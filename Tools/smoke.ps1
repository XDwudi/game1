param([switch]$Visual, [switch]$Revision, [switch]$Presentation, [switch]$ArtReview)
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskExe = Join-Path $taskRepo 'Builds/Release/Tidebreak.exe'
$taskLog = Join-Path $taskRepo 'Artifacts/player-smoke.log'
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Build the game first with Tools/build.ps1.' }
$taskArgs = '-tidebreakSmoke -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "{0}"' -f $taskLog
if ($ArtReview) { $taskArgs += ' -tidebreakArtReview' }
elseif ($Visual -or $Presentation) { $taskArgs += ' -tidebreakPresentation' }
else { $taskArgs += ' -tidebreakRevision' }
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Integration test returned $($taskProcess.ExitCode). See Artifacts/player-smoke.log." }
Get-Content -LiteralPath (Join-Path $taskRepo 'Builds/Artifacts/ExpeditionQA/results.txt')
