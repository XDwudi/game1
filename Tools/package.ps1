$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskBuild = Join-Path $taskRepo 'Builds/Release'
$taskOutput = Join-Path $taskRepo 'Builds/Tidebreak-v0.2.0-Windows-x64.zip'
if (-not (Test-Path -LiteralPath (Join-Path $taskBuild 'Tidebreak.exe'))) { throw 'Build the game first.' }
Copy-Item -LiteralPath (Join-Path $taskRepo 'Documentation/PLAY.txt') -Destination (Join-Path $taskBuild '开始游戏.txt') -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'ThirdParty/OFL-NotoSans.txt') -Destination (Join-Path $taskBuild 'OFL-NotoSans.txt') -Force
Compress-Archive -LiteralPath $taskBuild -DestinationPath $taskOutput -Force
Get-FileHash -LiteralPath $taskOutput -Algorithm SHA256
Get-Item -LiteralPath $taskOutput | Select-Object FullName, Length
