param([ValidateSet('All','UI','Models','Effects')][string]$Mode='All')
$ErrorActionPreference = 'Stop'
$Mode = @{'all'='All';'ui'='UI';'models'='Models';'effects'='Effects'}[$Mode.ToLowerInvariant()]
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskExe = Join-Path $taskRepo 'Builds/Release/Tidebreak.exe'
$taskStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$taskLabel = "$taskStamp-$Mode"
$taskLog = Join-Path $taskRepo "Artifacts/v07-$taskLabel.log"
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Build the game first with Tools/build.ps1.' }
$taskEvidence = Join-Path $taskRepo "Builds/Artifacts/V07QA/$taskLabel"
New-Item -ItemType Directory -Path $taskEvidence -Force | Out-Null
$taskBinary = Get-Item -LiteralPath $taskExe
$taskAssembly = Join-Path $taskRepo 'Builds/Release/Tidebreak_Data/Managed/Assembly-CSharp.dll'
$taskBuildIdentity = [ordered]@{
    run = $taskLabel
    mode = $Mode
    timescale = 1
    exe_sha256 = (Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash
    exe_modified_utc = $taskBinary.LastWriteTimeUtc.ToString('o')
    assembly_sha256 = $(if (Test-Path -LiteralPath $taskAssembly) { (Get-FileHash -LiteralPath $taskAssembly -Algorithm SHA256).Hash } else { 'not-present' })
    generated_utc = [DateTime]::UtcNow.ToString('o')
}
$taskBuildIdentity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskEvidence 'build-identity.json') -Encoding UTF8
$taskArgs = '-tidebreakSmoke -tidebreakV07 -v07Mode {0} -v07Run {1} -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "{2}"' -f $Mode,$taskLabel,$taskLog
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
$taskResults = Join-Path $taskRepo "Builds/Artifacts/V07QA/$taskLabel/results.txt"
if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults }
if ($taskProcess.ExitCode -ne 0) { throw "V07 tests returned $($taskProcess.ExitCode). Failure evidence is retained at $taskLog and $taskResults." }
