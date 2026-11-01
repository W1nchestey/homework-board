$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    throw "Cannot find the .NET Framework 4.x C# compiler (csc.exe)."
}

& (Join-Path $root "build\make-icon.ps1") | Out-Null

$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$sources = Get-ChildItem -Path (Join-Path $root "src") -Filter "*.cs" |
    Sort-Object Name |
    ForEach-Object { $_.FullName }

$arguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/debug-",
    "/langversion:5",
    "/codepage:65001",
    ("/win32icon:" + (Join-Path $root "build\app.ico")),
    ("/win32manifest:" + (Join-Path $root "src\app.manifest")),
    ("/resource:" + (Join-Path $root "build\app.ico") + ",HomeworkBoard.app.ico"),
    ("/out:" + (Join-Path $dist "HomeworkBoard.exe")),
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Xml.dll"
)
$arguments += $sources

Push-Location $root
try {
    & $csc $arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

$exe = Get-Item (Join-Path $dist "HomeworkBoard.exe")
"Build succeeded: {0} ({1:N0} bytes)" -f $exe.FullName, $exe.Length
