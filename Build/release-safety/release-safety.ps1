<#
Release-safety rehearsal for the WPF version, on a Windows machine: real Squirrel installs and
updates from a local feed, under a separate app id (YAPA2Test) so a real YAPA2 install is never
touched. Each action is one step; Build\release-safety\test-release-safety.sh runs the scenarios.

  release-safety.ps1 build   -Version 9.0.1 [-Broken]   build Release, pack, releasify into the feed
  release-safety.ps1 install -Version 9.0.1             run that version's Setup.exe as the logged-on user
  release-safety.ps1 start   [-Arguments /crashtest]    start the installed YAPA as the logged-on user
  release-safety.ps1 stop                                kill every YAPA.exe (and its crash dialog)
  release-safety.ps1 status                              what is installed, running and logged
  release-safety.ps1 measure                             how long a second start (/stop) takes, with and without crash reporting
  release-safety.ps1 crash-mode -Mode Ask|Always|Never    set "Crash reports" in Documents\YAPA2\settings.json
  release-safety.ps1 answer -Choice Send|DontSend [-Remember]   answer the crash report window (UI Automation)
  release-safety.ps1 backup | restore                    Documents\YAPA2 before and after
  release-safety.ps1 clean                               uninstall YAPA2Test, remove the feed

A -Broken build leaves Autofac.dll out of the package: the kind of bad release that crashes while
starting. YAPA_UPDATE_URL (the feed) and YAPA_CRASH_DSN are passed to the app from the environment
of this script (set them before calling start or install).
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet("build", "install", "start", "stop", "status", "measure", "crash-mode", "answer", "backup", "restore", "clean")][string]$Action,
    [ValidateSet("Ask", "Always", "Never")][string]$Mode,
    [ValidateSet("Send", "DontSend")][string]$Choice,
    [switch]$Remember,
    [string]$Version,
    [switch]$Broken,
    [string]$Arguments = "",
    [string]$Repo,
    [string]$Work = "C:\yapa-work\release-safety"
)
$ErrorActionPreference = "Stop"
#Windows PowerShell 5.1 has no $PSScriptRoot in parameter defaults
if (-not $Repo) { $Repo = (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "..\..")).Path }
$AppId = "YAPA2Test"
$Feed = Join-Path $Work "feed"
$Setups = Join-Path $Work "setups"

function Get-LoggedOnUser {
    #The user of the console session: installs and windows go there, not to SYSTEM
    $owner = (Get-CimInstance Win32_Process -Filter "Name='explorer.exe'" | Select-Object -First 1 | Invoke-CimMethod -MethodName GetOwner)
    return $owner.User
}

function Invoke-AsLoggedOnUser([string]$CommandLine, [int]$WaitSeconds = 0) {
    #A scheduled task for the Users group runs in the logged-on user's session, on their desktop
    $script = Join-Path $Work "as-user.cmd"
    $lines = @("@echo off")
    foreach ($name in "YAPA_UPDATE_URL", "YAPA_CRASH_DSN") {
        $value = [Environment]::GetEnvironmentVariable($name)
        if ($value) { $lines += "set $name=$value" }
    }
    $lines += $CommandLine
    Set-Content -Path $script -Value $lines -Encoding ASCII
    $task = "yapa-release-safety"
    $action = New-ScheduledTaskAction -Execute "cmd.exe" -Argument "/c `"$script`""
    $principal = New-ScheduledTaskPrincipal -GroupId "BUILTIN\Users" -RunLevel Limited
    Register-ScheduledTask -TaskName $task -Action $action -Principal $principal -Force | Out-Null
    Start-ScheduledTask -TaskName $task
    if ($WaitSeconds -gt 0) {
        $until = (Get-Date).AddSeconds($WaitSeconds)
        while ((Get-ScheduledTask -TaskName $task).State -eq "Running" -and (Get-Date) -lt $until) { Start-Sleep -Milliseconds 500 }
    }
}

function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
}

function Build-Version {
    if (-not $Version) { throw "build needs -Version" }
    New-Item -ItemType Directory -Force -Path $Feed, $Setups | Out-Null
    $release = Join-Path $Repo "Release"
    if (Test-Path $release) { Remove-Item $release -Recurse -Force }

    #The version for this test build; GitVersion is switched off for it
    $versionFile = Join-Path $Repo "YAPA\ReleaseSafetyVersion.cs"
    Set-Content $versionFile -Encoding UTF8 -Value @"
[assembly: System.Reflection.AssemblyVersion("$Version.0")]
[assembly: System.Reflection.AssemblyFileVersion("$Version.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("$Version")]
"@
    #The logged-on user's package cache: when this runs as SYSTEM (a remote shell), its own cache is empty
    if (-not $env:NUGET_PACKAGES) { $env:NUGET_PACKAGES = "C:\Users\$User\.nuget\packages" }
    try {
        $msbuild = Find-MSBuild
        & $msbuild (Join-Path $Repo "YAPA.sln") -restore -nodeReuse:false -p:Configuration=Release -p:DisableGitVersionTask=true -p:ReleaseSafetyTest=true -v:minimal -nologo | Out-File (Join-Path $Work "build-$Version.log")
        if ($LASTEXITCODE -ne 0) { throw "build $Version failed, see build-$Version.log" }
    }
    finally {
        Remove-Item $versionFile -Force -ErrorAction SilentlyContinue
    }

    if ($Broken) {
        #A bad release: a library the start needs is missing from the package
        Remove-Item (Join-Path $release "Autofac.dll") -Force
    }

    #The release nuspec, under the test app id
    $nuspec = Join-Path $Repo "Build\$AppId.nuspec"
    (Get-Content (Join-Path $Repo "Build\YAPA2.nuspec") -Raw) -replace "<id>YAPA2</id>", "<id>$AppId</id>" -replace "<title>YAPA2</title>", "<title>$AppId</title>" | Set-Content $nuspec -Encoding UTF8
    Push-Location (Join-Path $Repo "Build")
    try {
        & (Join-Path $Repo ".nuget\NuGet.exe") pack $nuspec -Version $Version -OutputDirectory $Work -NoPackageAnalysis | Out-Null
        $nugetPackages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { "C:\Users\$User\.nuget\packages" }
        $squirrel = Join-Path $nugetPackages "squirrel.windows\1.5.28\tools\Squirrel.exe"
        $package = Join-Path $Work "$AppId.$Version.nupkg"
        $p = Start-Process $squirrel -ArgumentList "--releasify `"$package`" --releaseDir=`"$Feed`" --no-msi" -Wait -PassThru -NoNewWindow
        if ($p.ExitCode -ne 0) { throw "releasify $Version failed" }
        Copy-Item (Join-Path $Feed "Setup.exe") (Join-Path $Setups "Setup-$Version.exe") -Force
    }
    finally {
        Pop-Location
        Remove-Item $nuspec -Force -ErrorAction SilentlyContinue
    }
    "built $Version$(if ($Broken) { ' (broken: no Autofac.dll)' })"
}

function Show-Status {
    "installed:"
    if (Test-Path $InstallDir) {
        Get-ChildItem $InstallDir -Directory -Filter "app-*" | ForEach-Object { "  $($_.Name)" }
    } else { "  (nothing)" }
    "running:"
    Get-CimInstance Win32_Process -Filter "Name='YAPA.exe'" | ForEach-Object {
        $v = try { (Get-Item $_.ExecutablePath).VersionInfo.FileVersion } catch { "?" }
        "  pid $($_.ProcessId) $v $($_.ExecutablePath) $($_.CommandLine.Substring([Math]::Min($_.CommandLine.Length, $_.CommandLine.IndexOf('.exe') + 5)))"
    }
    $lastStart = Join-Path $Work "last-start.txt"
    if (Test-Path $lastStart) { "last start: $(Get-Content $lastStart)" }
    "crash reports waiting to be sent:"
    $cache = Join-Path $Data "crash-reports"
    $waiting = if (Test-Path $cache) { @(Get-ChildItem $cache -Recurse -File -Filter "*.envelope") } else { @() }
    "  $($waiting.Count)"
    "crash report setting:"
    $settingsFile = Join-Path $Data "settings.json"
    $mode = if (Test-Path $settingsFile) { (Get-Content $settingsFile -Raw | ConvertFrom-Json).CrashReporting.Mode } else { $null }
    "  $(if ($mode) { $mode } else { 'Ask (default)' })"
    "log:"
    if (Test-Path $Log) {
        Get-Content $Log -Tail 400 | Where-Object { $_ -match "^\d{4}-\d\d-\d\d " } | Select-Object -Last 12 | ForEach-Object { "  " + $_.Substring(0, [Math]::Min(230, $_.Length)) }
    }
}

$User = Get-LoggedOnUser
$InstallDir = "C:\Users\$User\AppData\Local\$AppId"
$Data = "C:\Users\$User\Documents\YAPA2"
$Log = "C:\Users\$User\AppData\Local\YAPA2\Logs\log.log"

New-Item -ItemType Directory -Force -Path $Work | Out-Null
switch ($Action) {
    "build" { Build-Version }
    "install" {
        if (-not $Version) { throw "install needs -Version" }
        Invoke-AsLoggedOnUser "`"$(Join-Path $Setups "Setup-$Version.exe")`" --silent" -WaitSeconds 120
        "installed $Version"
    }
    "start" {
        Set-Content (Join-Path $Work "last-start.txt") (Get-Date -Format "yyyy-MM-dd HH:mm:ss.ffff")
        Invoke-AsLoggedOnUser "start `"`" `"$(Join-Path $InstallDir 'YAPA.exe')`" $Arguments"
        "started $Arguments"
    }
    "stop" {
        Get-Process YAPA, WerFault -ErrorAction SilentlyContinue | Stop-Process -Force
        "stopped"
    }
    "status" { Show-Status }
    "measure" {
        #A command launch (/start, /stop, /settings) is a second process that hands its arguments to the
        #running one and exits; time it with and without a crash report server set
        $result = Join-Path $Work "measure.txt"
        Remove-Item $result -ErrorAction SilentlyContinue
        $stub = Join-Path $InstallDir "YAPA.exe"
        $current = Get-ChildItem $InstallDir -Directory -Filter "app-*" | Sort-Object { [version]($_.Name.Substring(4)) } | Select-Object -Last 1
        $exe = Join-Path $current.FullName "YAPA.exe"
        $measure = Join-Path $Work "measure.ps1"
        Set-Content $measure -Encoding UTF8 -Value @"
`$lines = @()
foreach (`$dsn in @('$([Environment]::GetEnvironmentVariable("YAPA_CRASH_DSN"))', '')) {
    `$env:YAPA_CRASH_DSN = `$dsn
    `$times = foreach (`$i in 1..5) { (Measure-Command { Start-Process -FilePath '$exe' -ArgumentList '/stop' -Wait }).TotalMilliseconds }
    `$label = if (`$dsn) { 'with crash reporting' } else { 'without crash reporting' }
    `$lines += "{0}: median {1:N0} ms (runs: {2})" -f `$label, ((`$times | Sort-Object)[2]), ((`$times | ForEach-Object { '{0:N0}' -f `$_ }) -join ', ')
}
Set-Content '$result' `$lines
"@
        Invoke-AsLoggedOnUser "powershell -NoProfile -ExecutionPolicy Bypass -File `"$measure`"" -WaitSeconds 180
        Get-Content $result
    }
    "crash-mode" {
        $file = Join-Path $Data "settings.json"
        $json = if (Test-Path $file) { Get-Content $file -Raw | ConvertFrom-Json } else { New-Object PSObject }
        if (-not $json.PSObject.Properties["CrashReporting"]) { $json | Add-Member -NotePropertyName CrashReporting -NotePropertyValue (New-Object PSObject) }
        $json.CrashReporting | Add-Member -NotePropertyName Mode -NotePropertyValue $Mode -Force
        $json | ConvertTo-Json -Depth 20 -Compress | Set-Content $file -Encoding UTF8
        "crash reports: $Mode"
    }
    "answer" {
        #UI Automation has to run in the user's session, where the window is
        $result = Join-Path $Work "answer.txt"
        Remove-Item $result -ErrorAction SilentlyContinue
        $script = Join-Path $Work "answer.ps1"
        Set-Content $script -Encoding UTF8 -Value @"
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
`$A = [Windows.Automation.AutomationElement]
`$named = New-Object Windows.Automation.PropertyCondition(`$A::NameProperty, 'YAPA 2 crash report')
`$window = `$null
foreach (`$i in 1..120) { `$window = `$A::RootElement.FindFirst([Windows.Automation.TreeScope]::Descendants, `$named); if (`$window) { break }; Start-Sleep -Milliseconds 500 }
if (-not `$window) { Set-Content '$result' 'NO WINDOW'; exit }
`$texts = `$window.FindAll([Windows.Automation.TreeScope]::Descendants, (New-Object Windows.Automation.PropertyCondition(`$A::ControlTypeProperty, [Windows.Automation.ControlType]::Text))) | ForEach-Object { `$_.Current.Name }
function Find(`$id) { `$window.FindFirst([Windows.Automation.TreeScope]::Descendants, (New-Object Windows.Automation.PropertyCondition(`$A::AutomationIdProperty, `$id))) }
if ('$Remember' -eq 'True') { (Find 'RememberChoice').GetCurrentPattern([Windows.Automation.TogglePattern]::Pattern).Toggle() }
`$button = if ('$Choice' -eq 'Send') { 'SendButton' } else { 'DontSendButton' }
(Find `$button).GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern).Invoke()
Set-Content '$result' (@('WINDOW') + `$texts)
"@
        Invoke-AsLoggedOnUser "powershell -NoProfile -ExecutionPolicy Bypass -File `"$script`"" -WaitSeconds 90
        if (Test-Path $result) { Get-Content $result } else { "NO ANSWER" }
    }
    "backup" {
        $backup = Join-Path $Work "YAPA2-backup"
        if (Test-Path $backup) { Remove-Item $backup -Recurse -Force }
        Copy-Item $Data $backup -Recurse
        Get-ChildItem $Data -Recurse -File | ForEach-Object { "{0} {1}" -f (Get-FileHash $_.FullName).Hash, $_.FullName.Substring($Data.Length) } | Set-Content (Join-Path $Work "YAPA2-before.txt")
        Get-Content (Join-Path $Work "YAPA2-before.txt")
    }
    "restore" {
        $backup = Join-Path $Work "YAPA2-backup"
        Get-ChildItem $Data -Force | Where-Object { -not (Test-Path (Join-Path $backup $_.Name)) } | Remove-Item -Recurse -Force
        Copy-Item "$backup\*" $Data -Recurse -Force
        $after = Get-ChildItem $Data -Recurse -File | ForEach-Object { "{0} {1}" -f (Get-FileHash $_.FullName).Hash, $_.FullName.Substring($Data.Length) }
        if (Compare-Object (Get-Content (Join-Path $Work "YAPA2-before.txt")) $after) { "DIFFERENT after restore" } else { "restored: identical" }
    }
    "clean" {
        Get-Process YAPA, WerFault -ErrorAction SilentlyContinue | Stop-Process -Force
        $update = Join-Path $InstallDir "Update.exe"
        if (Test-Path $update) { Invoke-AsLoggedOnUser "`"$update`" --uninstall" -WaitSeconds 60 }
        Start-Sleep -Seconds 2
        if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force -ErrorAction SilentlyContinue }
        Unregister-ScheduledTask -TaskName "yapa-release-safety" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item $Feed, $Setups -Recurse -Force -ErrorAction SilentlyContinue
        "cleaned"
    }
}
