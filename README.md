![Yapa2](https://user-images.githubusercontent.com/7883111/155704469-7854e3d0-9f1e-4d46-b42a-acaf09856f57.jpg)

YAPA-2 is minimalistic desktop timer app for Pomodoro Technique users.

[![Build status](https://ci.appveyor.com/api/projects/status/p7k6ort2xvl35vbq?svg=true)](https://ci.appveyor.com/project/floatas/yapa-2-lffj9) [![Total downloads](https://img.shields.io/github/downloads/YetAnotherPomodoroApp/YAPA-2/total)](https://somsubhra.github.io/github-release-stats/?username=YetAnotherPomodoroApp&repository=YAPA-2&page=1&per_page=5) [Download latest version](https://github.com/YetAnotherPomodoroApp/YAPA-2/releases/latest) | [Website](https://yapa2.app)

Features
---------

- Pomodoro™ counter :)
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
![Theme Settings Preview](http://imgur.com/ulwYfix.gif)

  - Motivational theme
    - More about theme [here](https://github.com/YetAnotherPomodoroApp/MotivationalTheme).

Contribution guidelines
===
1. Before doing code refactoring, create issue to discuss it.
2. Create new branch for each bug/feature.

How to build installer
===
Installers are created with slightly modified [Squirrel.Windows](https://github.com/floatas/Squirrel.Windows)
1. `cd Build`
2. Restore nuget packages.
``NuGet restore ..\Yapa.sln``
2. Run ``BuildAndRelease.cmd`` 
Without arguments 2.0.0 version will be used.
To create specific version: ``BuildAndRelease.cmd 2.4.9`` 

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
