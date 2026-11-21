<#
Makes a release and uploads it only when every check has passed.

  powershell -ExecutionPolicy Bypass -File Build\release.ps1              build, test, package, gate; upload nothing
  powershell -ExecutionPolicy Bypass -File Build\release.ps1 -Publish     ... and upload when all passed
  powershell -ExecutionPolicy Bypass -File Build\release.ps1 -RollbackTo 2.0.190 [-Publish]
        re-release a good earlier version under the next version number. Squirrel never moves users
        to a lower version, so going back means publishing the old build as a new one.

Steps: Release build (Build.cmd) -> unit tests -> package and releasify (CreateNuget.ps1) ->
release gate (release-gate.ps1: the live version updates to this one and it runs) -> upload
(UploadRelease.ps1, needs INSTALLER_PUBLISH_KEY). Any failure stops before the upload.

Also run Build\release-safety\test-release-safety.sh after changes to start-up, updating or
crash reporting: it rehearses a broken release, the fallback and the fix.

The gate installs YAPA 2 for the current Windows user, so run this as a user without YAPA 2
installed (a test account or a VM).
#>
param(
    [switch]$Publish,
    [string]$RollbackTo,
    [string]$Server = "https://app.floatas.net/installers/YAPA-2/"
)
$ErrorActionPreference = "Stop"
$Build = Split-Path -Parent $MyInvocation.MyCommand.Path
$Repo = Split-Path -Parent $Build
$Output = Join-Path $Repo "Release"

function Step([string]$text) { Write-Output ""; Write-Output "== $text" }
function Fail([string]$text) { Write-Output "STOPPED, nothing uploaded: $text"; exit 1 }

$version = $null
if ($RollbackTo) {
    Step "rollback: $RollbackTo as a new version"
    #Invoke-WebRequest in PowerShell 5.1 gives bytes, not text, for RELEASES
    $client = New-Object System.Net.WebClient
    $live = ($client.DownloadString($Server + "RELEASES") -split "`n" |
        ForEach-Object { if ($_ -match "YAPA2-([\d\.]+)-full\.nupkg") { [version]$Matches[1] } } | Sort-Object | Select-Object -Last 1)
    if (-not $live) { Fail "could not read the live version from $($Server)RELEASES" }
    $version = "$($live.Major).$($live.Minor).$($live.Build + 1)"
    $package = Join-Path $env:TEMP "YAPA2-$RollbackTo-full.nupkg"
    $client.DownloadFile($Server + "YAPA2-$RollbackTo-full.nupkg", $package)
    #The files of the good version, where a build would have put them
    if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }
    $unpacked = Join-Path $env:TEMP "YAPA2-$RollbackTo-unpacked"
    if (Test-Path $unpacked) { Remove-Item $unpacked -Recurse -Force }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($package, $unpacked)
    Copy-Item (Join-Path $unpacked "lib\net45") $Output -Recurse
    #Squirrel made these stubs when it released the old version, and makes them again now
    Get-ChildItem $Output -Recurse -Filter "*_ExecutionStub.exe" | Remove-Item -Force
    Write-Output "$RollbackTo goes out as $version (live is $live)"
}
else {
    Step "Release build"
    & cmd /c (Join-Path $Build "Build.cmd")
    if ($LASTEXITCODE -ne 0) { Fail "the build failed" }

    Step "unit tests"
    & dotnet test (Join-Path $Repo "YAPA.Shared.Tests\YAPA.Shared.Tests.csproj") --nologo -v q
    if ($LASTEXITCODE -ne 0) { Fail "unit tests failed" }
}

Step "package and releasify"
$releaseDir = Join-Path $Build "Release"
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Push-Location $Build
try {
    if ($version) { & (Join-Path $Build "CreateNuget.ps1") -Version $version } else { & (Join-Path $Build "CreateNuget.ps1") }
}
catch {
    Fail "packaging failed: $_"
}
finally {
    Pop-Location
    #CreateNuget writes the version into the nuspec; keep the file as it is in git
    & git -C $Repo checkout -- Build/YAPA2.nuspec 2>$null
}

if (-not (Get-ChildItem $releaseDir -Filter "YAPA2.*.exe")) { Fail "Squirrel made no Setup (see SquirrelSetup.log next to Squirrel.exe)" }

Step "release gate"
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Build "release-gate.ps1") -ReleaseDir $releaseDir -Server $Server
if ($LASTEXITCODE -ne 0) { Fail "the release gate failed" }

if (-not $Publish) {
    Step "ready: every check passed. Run again with -Publish to upload, or upload $releaseDir with UploadRelease.ps1"
    exit 0
}

Step "upload"
if (-not $env:INSTALLER_PUBLISH_KEY) { Fail "INSTALLER_PUBLISH_KEY is not set" }
Push-Location $Build
try { & (Join-Path $Build "UploadRelease.ps1") } finally { Pop-Location }
Write-Output "Uploaded. Watch the crash reports for the next day; Build\release.ps1 -RollbackTo <good version> -Publish goes back."
