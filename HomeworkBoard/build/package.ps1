$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dist = Join-Path $root "dist"
$stage = Join-Path $root "build\release"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item (Join-Path $dist "HomeworkBoard.exe") $stage
Get-ChildItem $dist -Filter "*.txt" | ForEach-Object { Copy-Item $_.FullName $stage }
Get-ChildItem $dist -Filter "*.png" | ForEach-Object { Copy-Item $_.FullName $stage }

$zip = Join-Path $root "HomeworkBoard_v1.0.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip)
Get-Item $zip | Select-Object FullName, Length
