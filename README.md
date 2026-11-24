![Yapa2](https://user-images.githubusercontent.com/7883111/155704469-7854e3d0-9f1e-4d46-b42a-acaf09856f57.jpg)

YAPA-2 is minimalistic desktop timer app for Pomodoro Technique users.

[![Build status](https://ci.appveyor.com/api/projects/status/p7k6ort2xvl35vbq?svg=true)](https://ci.appveyor.com/project/floatas/yapa-2-lffj9) [![Total downloads](https://img.shields.io/github/downloads/YetAnotherPomodoroApp/YAPA-2/total)](https://somsubhra.github.io/github-release-stats/?username=YetAnotherPomodoroApp&repository=YAPA-2&page=1&per_page=5) [Download latest version](https://github.com/YetAnotherPomodoroApp/YAPA-2/releases/latest) | [Website](https://yapa2.app)

Features
---------

- Pomodoro™ counter :)
  - Choose what it shows in Settings → General → "Display count of":
    - Pomodoro index: position in the current round (1, 2, 3 ...); the last pomodoro of a round is followed by the long break
    - Completed this session: pomodoros completed since the application was started
    - Completed today: pomodoros completed today, from the history shown in Dashboard
- Control app using taskbar jumplist
- Configurable periods
- Count time backwards
- Automatically start break
- Show\hide in taskbar
- Minimize to tray
- Supports custom themes

- Command line arguments
  - /start
  - /stop
  - /pause
  - /reset
  - /settings
  - /homepage

- Sound
  - Disable all sound notifications
  - Volume controls
  - Play custom music during work\break periods

- Dashboard
  - Pomodoro counter history similar to github contributions

- Themes
  - YAPA 1.0 theme
    - Shows period progress on taskbar
    - Select opacity for timer
    - Select light or dark theme for timer
    - Change application size
    - Enable\Disable flashing animation
    - Optional background behind the timer (color and opacity) to keep it readable on any wallpaper or window
    - The status text and buttons have the same shadow as the digits, so they read on any background
![Theme Settings Preview](http://imgur.com/ulwYfix.gif)

  - Motivational theme
    - More about theme [here](https://github.com/YetAnotherPomodoroApp/MotivationalTheme).

Accessibility
---------

YAPA 2 is built to work with screen readers such as NVDA, JAWS and Narrator, and can be used with the keyboard alone. In the timer window, `F1` says all of this.

The timer speaks when it changes: work started, paused, resumed and finished, break started and finished. To hear where it is:

| | NVDA | JAWS | Narrator |
|---|---|---|---|
| Time left, to the second (YAPA's own key, in the timer window) | `Ctrl+T` | `Ctrl+T` | `Ctrl+T` |
| Window title: the minutes left | `Insert+T` | `Insert+T` | `Narrator+T` |
| Status bar: the time left, to the second | `Insert+End` | `Insert+Page Down` | |
| Tray icon: the minutes left | `Win+B`, then the arrow keys | `Win+B` | `Win+B` |

The screen reader also reads the title when you switch to YAPA with `Alt+Tab`. The tray icon is there only while YAPA is minimized to the tray, unless Settings → System tray → "Always show tray icon" is on. Windows 11 may put it under "Show hidden icons", which `Win+B` and the arrow keys also reach.

In the timer window:
- `Tab` moves between the timer buttons
- The `Menu` key or `Shift+F10` opens a menu with every command, including Settings
- `F1` says the keyboard help

In Settings, `Tab` goes from the list of pages to the page, then to Close and Save, and `Enter` saves. Every control has a spoken label, and the dashboard calendar can be read day by day.

For a timer that is easier to see, give it a background in Settings → Themes.

Crash reports
---------

When YAPA 2 closes because of an error, it keeps a crash report on your computer. The next time it starts, it shows you what the report contains and asks whether to send it. Nothing is sent without your say.

- A report contains the error, where in YAPA it happened, the YAPA, Windows and .NET versions, and the last few things YAPA did before the error
- It does not contain your name, your computer's name, your settings or your pomodoro history
- "Show everything" in that window shows the whole report, exactly as it would be sent
- Reports go to our own server, bugs.floatas.net, and are used only to fix the problem
- Settings → About → Crash reports: *Ask before sending* (the default), *Send without asking*, or *Don't send*
- Reports waiting to be sent are in `Documents\YAPA2\crash-reports`

Windows, macOS and Linux (Avalonia)
===
`YAPA.Avalonia` is the same application on [Avalonia](https://avaloniaui.net), so it also runs on macOS and Linux.
It uses `YAPA.Shared` (the engine, settings and commands) as it is and recreates the WPF windows:
the YAPA 1.0 timer looks the same (borderless, see-through, Segoe UI Light with the soft shadow), and
the settings window keeps the WPF layout with [Material.Avalonia](https://github.com/AvaloniaCommunity/Material.Avalonia)
in place of MaterialDesignInXaml.

Settings, history and the snapshot of an unfinished pomodoro are read from and written to the same
`Documents/YAPA2` folder as the Windows version (set `YAPA_HOME` to use another folder).

Build and run with the .NET 8 SDK or newer:
```
dotnet run --project YAPA.Avalonia
```
`YAPA.Avalonia.sln` holds the projects that build on every system (`YAPA.sln` is the WPF version).

The screen reader and keyboard support described under Accessibility is in the Avalonia version too
(on macOS `Cmd+T` reads the timer). It is tested with Avalonia's automation API, not yet with VoiceOver
or Orca themselves.

Not in the Avalonia version: crash reports, the release notes window, taskbar progress and jump lists (Windows only),
and loading themes or plugins from DLLs next to the app (they are compiled in). System tray balloon tips
are shown as system notifications. Besides the WPF command line arguments it understands `/exit`.

On Linux the app needs the usual desktop libraries and `libssl` (present on desktop systems).

### Updates
The Avalonia version updates itself with [Velopack](https://velopack.io), the successor of Squirrel, the
same way the WPF version does: at start it looks for a new version (GitHub releases of this project;
pre-releases too with `PreRelease.txt`), downloads it in the background, shows "Restart application to
apply updates" in the settings window, and installs it when YAPA closes. A downloaded update that was
not installed (YAPA was killed) is installed at the next start. Each step is written to `update.log`
in the data folder. `YAPA_UPDATE_URL` points at another feed, a web address or a local folder.

Releases are made with the `vpk` tool (`dotnet tool install -g vpk`):
```
dotnet publish YAPA.Avalonia -c Release -r osx-arm64 --self-contained -p:Version=2.1.0 -o publish
vpk pack -u YAPA2 -v 2.1.0 -p publish -e YAPA --packTitle "YAPA 2" -o releases
```
For macOS releases add `--signAppIdentity "Developer ID Application: ..."`, `--signInstallIdentity
"Developer ID Installer: ..."` and `--notaryProfile <profile saved with xcrun notarytool store-credentials>`;
without them macOS warns about a downloaded app. Linux AppImages are packed from a Mac or Linux machine
with `vpk "[linux]" pack -r linux-x64 ...` (needs `mksquashfs`).

`Build/test-updates-macos.sh` and `Build/test-updates-linux.sh` (Docker) test updating end to end on
this machine: they pack two versions into a local feed, install the older one and check that it
downloads the newer one, is left alone while it runs, installs the update when closed (or at the next
start when killed), finds itself up to date afterwards, and only logs an unreachable feed.

### Tests
`YAPA.Avalonia.Tests` runs the real windows headless and goes through every settings page and the timer
buttons, checking `settings.json` and what the timer shows: `dotnet test YAPA.Avalonia.Tests`.

Contribution guidelines
===
1. Before doing code refactoring, create issue to discuss it.
2. Create new branch for each bug/feature.

How to build installer
===
Installers are created with slightly modified [Squirrel.Windows](https://github.com/floatas/Squirrel.Windows)
1. Build the Release configuration. This also restores the NuGet packages.
``Build\Build.cmd``
2. Run ``Build\BuildAndRelease.cmd`` 
Without arguments 2.0.0 version will be used.
To create specific version: ``Build\BuildAndRelease.cmd 2.4.9`` 

Nightly builds
===
To enable nightly builds:
1. Go to folder: C:\Users\<username>\Documents\YAPA2
2. Create empty file: PreRelease.txt
3. Restart application.
4. Open application settings, next to version you should see *pre-release*.

To disable nightly builds:
1. Go to folder: C:\Users\<username>\Documents\YAPA2
2. Remove file: PreRelease.txt
3. Uninstall application (application settings and completed pomodoro history will not be removed).
4. Install latest stable version
