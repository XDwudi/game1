param([ValidateSet('All','Systems','Bosses','Negative','Campaign','Coastal')][string]$Mode='All',[int]$Boss=0,[float]$Speed=2)
$ErrorActionPreference = 'Stop'
$Mode = @{'all'='All';'systems'='Systems';'bosses'='Bosses';'negative'='Negative';'campaign'='Campaign';'coastal'='Coastal'}[$Mode.ToLowerInvariant()]
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskExe = Join-Path $taskRepo 'Builds/Release/Tidebreak.exe'
$taskStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$taskLabel = "$taskStamp-$Mode-$Boss"
$taskLog = Join-Path $taskRepo "Artifacts/v05-$taskLabel.log"
if (-not (Test-Path -LiteralPath $taskExe)) { throw 'Build the game first with Tools/build.ps1.' }
if ($Boss -ne 0 -and ($Boss -lt 108 -or $Boss -gt 118)) { throw 'Boss must be 0 (all) or a species ID from 108 through 118.' }
$taskSpeed = $Speed.ToString([System.Globalization.CultureInfo]::InvariantCulture)
$taskEvidence = Join-Path $taskRepo "Builds/Artifacts/V05QA/$taskLabel"
New-Item -ItemType Directory -Path $taskEvidence -Force | Out-Null
$taskBinary = Get-Item -LiteralPath $taskExe
$taskAssembly = Join-Path $taskRepo 'Builds/Release/Tidebreak_Data/Managed/Assembly-CSharp.dll'
$taskBuildIdentity = [ordered]@{
    run = $taskLabel
    mode = $Mode
    boss = $Boss
    timescale = $Speed
    exe_sha256 = (Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash
    exe_modified_utc = $taskBinary.LastWriteTimeUtc.ToString('o')
    assembly_sha256 = $(if (Test-Path -LiteralPath $taskAssembly) { (Get-FileHash -LiteralPath $taskAssembly -Algorithm SHA256).Hash } else { 'not-present' })
    generated_utc = [DateTime]::UtcNow.ToString('o')
}
$taskBuildIdentity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskEvidence 'build-identity.json') -Encoding UTF8
$taskArgs = '-tidebreakSmoke -tidebreakV05 -v05Mode {0} -v05Boss {1} -v05Speed {2} -v05Run {3} -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "{4}"' -f $Mode,$Boss,$taskSpeed,$taskLabel,$taskLog
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
$taskResults = Join-Path $taskRepo "Builds/Artifacts/V05QA/$taskLabel/results.txt"
if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults }
if ($taskProcess.ExitCode -ne 0) { throw "V05 tests returned $($taskProcess.ExitCode). Failure evidence is retained at $taskLog and $taskResults." }
