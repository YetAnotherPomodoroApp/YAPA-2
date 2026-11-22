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

YAPA 2 is built to work with screen readers such as Narrator and NVDA, and can be used with the keyboard alone.

- The timer speaks when it changes: work started, paused, resumed and finished, break started and finished
- `Ctrl+T` reads where the timer is
- `Tab` moves between the timer buttons
- The `Menu` key or `Shift+F10` opens a menu with every command, including Settings
- In Settings, `Tab` goes from the list of pages to the page, then to Close and Save; `Enter` saves
- Every control in Settings has a spoken label, and the dashboard calendar can be read day by day
- With the System tray plugin, the tooltip of the tray icon tells the remaining time
- For a timer that is easier to see, give it a background in Settings → Themes

Crash reports
---------

When YAPA 2 closes because of an error, it keeps a crash report on your computer. The next time it starts, it shows you what the report contains and asks whether to send it. Nothing is sent without your say.

- A report contains the error, where in YAPA it happened, the YAPA, Windows and .NET versions, and the last few things YAPA did before the error
- It does not contain your name, your computer's name, your settings or your pomodoro history
- "Show everything" in that window shows the whole report, exactly as it would be sent
- Reports go to our own server, bugs.floatas.net, and are used only to fix the problem
- Settings → About → Crash reports: *Ask before sending* (the default), *Send without asking*, or *Don't send*
- Reports waiting to be sent are in `Documents\YAPA2\crash-reports`

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
