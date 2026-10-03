param([string]$UnityPath = 'E:/unity/2021.3.16f1c1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskProject = Join-Path $taskRepo 'Tidebreak'
$taskArtifacts = Join-Path $taskRepo 'Artifacts'
New-Item -ItemType Directory -Path $taskArtifacts -Force | Out-Null
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity editor not found: $UnityPath" }
$taskArgs = '-batchmode -quit -projectPath "{0}" -executeMethod Tidebreak.Editor.ProjectBuilder.BuildWindows -logFile "{1}"' -f $taskProject, (Join-Path $taskArtifacts 'build.log')
$taskProcess = Start-Process -FilePath $UnityPath -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Unity returned $($taskProcess.ExitCode). See Artifacts/build.log." }
Get-Content -LiteralPath (Join-Path $taskArtifacts 'build-result.txt')
