param([ValidateSet('All','Bosses','Effects','Display')][string]$Mode='All',
    [ValidateSet('Roundtrip','SeedRestart','ResumeRestart')][string]$DisplayStep='Roundtrip')
$ErrorActionPreference = 'Stop'
$Mode = @{'all'='All';'bosses'='Bosses';'effects'='Effects';'display'='Display'}[$Mode.ToLowerInvariant()]
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskExe = Join-Path $taskRepo 'Builds/Release/Tidebreak.exe'
$taskStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$taskLabel = "$taskStamp-$Mode-$DisplayStep"
$taskLog = Join-Path $taskRepo "Artifacts/v08-$taskLabel.log"
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Build the game first with Tools/build.ps1.' }
if (Get-Process Tidebreak,Unity -ErrorAction SilentlyContinue) { throw 'Close the running player or Editor before starting a standalone validation.' }
$taskEvidence = Join-Path $taskRepo "Builds/Artifacts/V08QA/$taskLabel"
New-Item -ItemType Directory -Path $taskEvidence -Force | Out-Null
$taskAssembly = Join-Path $taskRepo 'Builds/Release/Tidebreak_Data/Managed/Assembly-CSharp.dll'
$taskBuildIdentity = [ordered]@{
    run = $taskLabel
    mode = $Mode
    display_step = $DisplayStep
    timescale = 1
    exe_sha256 = (Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash
    assembly_sha256 = (Get-FileHash -LiteralPath $taskAssembly -Algorithm SHA256).Hash
    generated_utc = [DateTime]::UtcNow.ToString('o')
}
$taskBuildIdentity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskEvidence 'build-identity.json') -Encoding UTF8
$taskArgs = '-tidebreakSmoke -tidebreakV08 -v08Mode {0} -v08Run {1} -v08DisplayStep {2} -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "{3}"' -f $Mode,$taskLabel,$DisplayStep,$taskLog
if ($DisplayStep -eq 'ResumeRestart') { $taskArgs += ' -v08DisplayResume' }
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
$taskResults = Join-Path $taskEvidence 'results.txt'
if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults } else { throw "No QA results were written. See $taskLog." }
if ($taskProcess.ExitCode -ne 0) { throw "V08 tests returned $($taskProcess.ExitCode). Evidence retained at $taskLog and $taskResults." }
