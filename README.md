YAPA 2
====
![](https://floatas.visualstudio.com/_apis/public/build/definitions/85dbe4af-5aee-4fc1-a0cb-40ddc5fcf2d6/3/badge)

YAPA-2 is minimalistic desktop timer app for Pomodoro Technique users. 

![](http://i.imgur.com/VkoEGe0.png)

Right click to open dashboard and settings:
![](http://i.imgur.com/lnhuPzx.png)

### [Download latest version!](https://github.com/YetAnotherPomodoroApp/YAPA-2/releases/latest)

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
1. Restore nuget packages.
``NuGet restore ..\Yapa.sln``
2. Run ``Build\BuildAndRelease.cmd`` 
Without arguments 2.0.0 version will be used.
To create specific version: ``Build\BuildAndRelease.cmd 2.4.9`` 
