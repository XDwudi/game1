param([ValidateSet('All','Systems','World','Combat','Campaign')][string]$Mode='All',[float]$Speed=2)
$ErrorActionPreference = 'Stop'
$Mode = @{'all'='All';'systems'='Systems';'world'='World';'combat'='Combat';'campaign'='Campaign'}[$Mode.ToLowerInvariant()]
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskExe = Join-Path $taskRepo 'Builds/Release/Tidebreak.exe'
$taskStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$taskLabel = "$taskStamp-$Mode"
$taskLog = Join-Path $taskRepo "Artifacts/v06-$taskLabel.log"
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Build the game first with Tools/build.ps1.' }
$taskSpeed = $Speed.ToString([System.Globalization.CultureInfo]::InvariantCulture)
$taskEvidence = Join-Path $taskRepo "Builds/Artifacts/V06QA/$taskLabel"
New-Item -ItemType Directory -Path $taskEvidence -Force | Out-Null
$taskBinary = Get-Item -LiteralPath $taskExe
$taskAssembly = Join-Path $taskRepo 'Builds/Release/Tidebreak_Data/Managed/Assembly-CSharp.dll'
$taskBuildIdentity = [ordered]@{
    run = $taskLabel
    mode = $Mode
    timescale = $Speed
    exe_sha256 = (Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash
    exe_modified_utc = $taskBinary.LastWriteTimeUtc.ToString('o')
    assembly_sha256 = $(if (Test-Path -LiteralPath $taskAssembly) { (Get-FileHash -LiteralPath $taskAssembly -Algorithm SHA256).Hash } else { 'not-present' })
    generated_utc = [DateTime]::UtcNow.ToString('o')
}
$taskBuildIdentity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskEvidence 'build-identity.json') -Encoding UTF8
$taskArgs = '-tidebreakSmoke -tidebreakV06 -v06Mode {0} -v06Speed {1} -v06Run {2} -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "{3}"' -f $Mode,$taskSpeed,$taskLabel,$taskLog
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
$taskResults = Join-Path $taskRepo "Builds/Artifacts/V06QA/$taskLabel/results.txt"
if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults }
if ($taskProcess.ExitCode -ne 0) { throw "V06 tests returned $($taskProcess.ExitCode). Failure evidence is retained at $taskLog and $taskResults." }
