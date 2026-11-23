<#
The last check before a release is uploaded: does the exact release in Build\Release update the
version users have today, and does it then run?

  1. installs the live version (its Setup from the update server) for this Windows user,
  2. updates it with Squirrel's Update.exe from Build\Release - the RELEASES file, delta and full
     package that would be uploaded,
  3. starts the new version and checks that it shows its window and keeps running, that its own
     update check reaches the update server, and that the previous version's folder is kept
     (the start-up fallback needs it),
  4. uninstalls and puts Documents\YAPA2 and the YAPA log back as they were.

Run it as a Windows user that doesn't have YAPA 2 installed (a test account or a VM): it refuses
to run where YAPA 2 is installed. Exit code 0 means safe to upload.

  powershell -ExecutionPolicy Bypass -File Build\release-gate.ps1 [-ReleaseDir Build\Release] [-LogFile gate.log]
#>
param(
    [string]$ReleaseDir,
    [string]$Server = "https://app.floatas.net/installers/YAPA-2/",
    [string]$LogFile
)
$ErrorActionPreference = "Stop"
if (-not $ReleaseDir) { $ReleaseDir = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "Release" }
$ReleaseDir = (Resolve-Path $ReleaseDir).Path
$Install = Join-Path $env:LOCALAPPDATA "YAPA2"
$Data = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "YAPA2"
$Log = Join-Path $Install "Logs\log.log"
$Work = Join-Path $env:TEMP "yapa-release-gate"
$Failures = New-Object System.Collections.Generic.List[string]

function Say([string]$text) {
    Write-Output $text
    if ($LogFile) { Add-Content -Path $LogFile -Value $text }
}
function Check([bool]$ok, [string]$what) {
    if ($ok) { Say "ok:   $what" } else { Say "FAIL: $what"; $Failures.Add($what) }
}
function Versions-In([string]$releases) {
    Get-Content $releases | ForEach-Object { if ($_ -match "YAPA2-([\d\.]+)-full\.nupkg") { [version]$Matches[1] } } | Sort-Object -Unique
}
function Running-YAPA {
    Get-Process YAPA -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$Install\*" }
}
function Wait-Window([int]$seconds) {
    $until = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $until) {
        $p = Running-YAPA | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($p) { return $p }
        Start-Sleep -Milliseconds 500
    }
    return $null
}
function Stop-YAPA {
    Running-YAPA | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}
function Read-Text([string]$url) {
    #Invoke-WebRequest in PowerShell 5.1 gives bytes, not text, for a file like RELEASES
    $client = New-Object System.Net.WebClient
    try { return $client.DownloadString($url) } finally { $client.Dispose() }
}
#Everything an install leaves outside its folder: Start menu and desktop entries, the uninstall record
$UninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\YAPA2"
function Install-Traces {
    $places = @([Environment]::GetFolderPath("Programs"), [Environment]::GetFolderPath("Desktop"))
    $traces = @($places | ForEach-Object { Get-ChildItem $_ -Recurse -ErrorAction SilentlyContinue } |
        Where-Object { $_.FullName -match "YAPA" } | ForEach-Object { $_.FullName })
    if (Test-Path $UninstallKey) { $traces += $UninstallKey }
    return $traces
}
function Log-Since([datetime]$since) {
    if (-not (Test-Path $Log)) { return @() }
    Get-Content $Log | Where-Object { $_ -match "^(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d)" -and [datetime]$Matches[1] -ge $since.AddSeconds(-1) }
}

# --- what is being released, and what users have now
$releases = Join-Path $ReleaseDir "RELEASES"
if (-not (Test-Path $releases)) { throw "No RELEASES in ${ReleaseDir}: run CreateNuget.ps1 first" }
$candidate = (Versions-In $releases | Select-Object -Last 1)
$liveReleases = (Read-Text ($Server + "RELEASES")) -split "`n"
$live = $liveReleases | ForEach-Object { if ($_ -match "YAPA2-([\d\.]+)-full\.nupkg") { [version]$Matches[1] } } | Sort-Object | Select-Object -Last 1
if (-not $live) { throw "Could not read the live version from $($Server)RELEASES" }
$candidateText = "$($candidate.Major).$($candidate.Minor).$($candidate.Build)"
$liveText = "$($live.Major).$($live.Minor).$($live.Build)"
Say "== release gate: live $liveText -> new $candidateText ($ReleaseDir)"
#The result depends on what the server holds right now
Say "   live RELEASES: $(($liveReleases | Where-Object { $_ -match 'nupkg' } | Select-Object -Last 3 | ForEach-Object { ($_ -split ' ')[1] }) -join ', ')"
if ($candidate -le $live) { throw "The new version $candidateText is not newer than the live $liveText" }

if (Test-Path (Join-Path $Install "Update.exe")) {
    throw "YAPA 2 is installed for this Windows user ($Install). Run the gate as a user without it (a test account or a VM), so a real install is never touched."
}

# --- keep what the user has, to put it back
if (Test-Path $Work) { Remove-Item $Work -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Work | Out-Null
$hadData = Test-Path $Data
if ($hadData) { Copy-Item $Data (Join-Path $Work "Documents-YAPA2") -Recurse }
#Crash reports waiting on this machine are set aside: the new version would ask about them (or, with
#"Send without asking", send them) during the test. They come back with the rest of Documents\YAPA2.
$waiting = Join-Path $Data "crash-reports"
if (Test-Path $waiting) { Remove-Item $waiting -Recurse -Force; Say "   (crash reports waiting in Documents\YAPA2 are set aside for the test)" }
$hadLogs = Test-Path (Join-Path $Install "Logs")
$tracesBefore = Install-Traces
if ($hadLogs) { Copy-Item (Join-Path $Install "Logs") (Join-Path $Work "Logs") -Recurse }

try {
    # --- 1. the live version, installed the way users got it
    $setup = Join-Path $Work "YAPA2.$liveText.exe"
    Invoke-WebRequest -UseBasicParsing -Uri ($Server + "YAPA2.$liveText.exe") -OutFile $setup
    Start-Process $setup -ArgumentList "--silent" -Wait
    #Setup may start the app when it is done: that copy would hold the single-instance lock
    $first = Wait-Window 20
    Stop-YAPA
    Check (Test-Path (Join-Path $Install "app-$liveText\YAPA.exe")) "the live $liveText installs from its Setup"

    # --- 2. update it with exactly the files that would be uploaded
    $update = Start-Process (Join-Path $Install "Update.exe") -ArgumentList "--update=`"$ReleaseDir`"" -Wait -PassThru
    Check ($update.ExitCode -eq 0) "Update.exe updated $liveText from Build\Release (exit code $($update.ExitCode))"
    Check (Test-Path (Join-Path $Install "app-$candidateText\YAPA.exe")) "app-$candidateText is installed"
    Check (Test-Path (Join-Path $Install "app-$liveText\YAPA.exe")) "app-$liveText is kept (the start-up fallback needs it)"

    # --- 3. the new version starts, keeps running and can update itself
    $started = Get-Date
    Start-Process (Join-Path $Install "YAPA.exe")
    $process = Wait-Window 90
    Check ($null -ne $process) "the new version shows its window"
    if ($process) {
        $version = (Get-Item $process.Path).VersionInfo.FileVersion
        Check ($process.Path -like "*app-$candidateText*") "the running copy is app-$candidateText (file version $version)"
        Start-Sleep -Seconds 30
        Check ($null -ne (Running-YAPA | Where-Object { $_.Id -eq $process.Id })) "it is still running 30 s later"
        $lines = Log-Since $started
        Check ([bool]($lines | Where-Object { $_ -match "Application starting" })) "it logged its start"
        #HTTPS since 2.0.191; a rollback to an older build checks over HTTP, as it did when it was live
        $updateLine = $lines | Where-Object { $_ -match "Updating from: (https?)://" } | Select-Object -First 1
        $scheme = if ($updateLine -match "Updating from: (https?)://") { $Matches[1].ToUpper() } else { "" }
        Check ([bool]$updateLine) "its update check went to the update server$(if ($scheme) { " ($scheme)" })"
        Check (-not ($lines | Where-Object { $_ -match "Update over HTTPS failed|Unhandled exception while updating" })) "its update check had no error"
        Check (-not ($lines | Where-Object { $_ -match " FATAL " })) "nothing fatal in its log"
        Check (-not ($lines | Where-Object { $_ -match "failed to start" })) "no fall back to the previous version"
    }
}
finally {
    Stop-YAPA
    $uninstaller = Join-Path $Install "Update.exe"
    if (Test-Path $uninstaller) { Start-Process $uninstaller -ArgumentList "--uninstall" -Wait }
    Start-Sleep -Seconds 2
    if (Test-Path $Install) { Remove-Item $Install -Recurse -Force -ErrorAction SilentlyContinue }
    if ($hadLogs) {
        New-Item -ItemType Directory -Force -Path $Install | Out-Null
        Copy-Item (Join-Path $Work "Logs") (Join-Path $Install "Logs") -Recurse -Force
    }
    if (Test-Path $Data) { Remove-Item $Data -Recurse -Force }
    if ($hadData) { Copy-Item (Join-Path $Work "Documents-YAPA2") $Data -Recurse }
    #Uninstalled for real, not only "Update.exe --uninstall returned": whatever the install added is gone.
    #Squirrel's uninstall leaves the empty Start menu folder (for users too); that is tidied, not a failure.
    $leftovers = @(Install-Traces | Where-Object { $tracesBefore -notcontains $_ })
    foreach ($item in ($leftovers | Sort-Object Length -Descending)) {
        if ($item -eq $UninstallKey) { Remove-Item $item -Recurse -Force -ErrorAction SilentlyContinue }
        elseif ((Test-Path $item -PathType Container) -and -not (Get-ChildItem $item -Force)) {
            Say "   (removed the empty folder Squirrel's uninstall leaves: $item)"
            Remove-Item $item -Force -ErrorAction SilentlyContinue
        }
    }
    $left = @(Install-Traces | Where-Object { $tracesBefore -notcontains $_ })
    if (Get-ChildItem $Install -Directory -Filter "app-*" -ErrorAction SilentlyContinue) { $left += "$Install\app-*" }
    Check ($left.Count -eq 0) "nothing of the test install is left on this machine$(if ($left) { ' (left: ' + ($left -join ', ') + ')' })"
    Say "   (uninstalled; Documents\YAPA2 and the YAPA log put back)"
}

if ($Failures.Count -gt 0) {
    Say "NOT SAFE TO UPLOAD: $($Failures.Count) check(s) failed"
    exit 1
}
Say "SAFE TO UPLOAD: $liveText updates to $candidateText and it runs"
exit 0
