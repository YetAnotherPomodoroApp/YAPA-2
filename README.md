YAPA 2
====
![](https://floatas.visualstudio.com/_apis/public/build/definitions/85dbe4af-5aee-4fc1-a0cb-40ddc5fcf2d6/3/badge)

YAPA-2 is minimalistic desktop timer app for Pomodoro Technique users. 

Features:
---------

- [x] Configurable periods
- [x] Sound control
- [x] Play custom song during work\break
- [x] Pomodoro™ counter :)
- [x] Minimize to tray
- [x] Hide in taskbar
- [x] Remember applications position
- [x] Control app using taskbar jumplist
- [x] Discover and load plugins
- [x] Disable individual plugins
- [x] Discover and load themes
- [x] Command line arguments

- [x] Dashboard
  - [x] Pomodoro counter history similar to github contributions

- [x] Move exising YAPA theme
  - [x] Shows period progress on taskbar
  - [x] Select opacity for timer
  - [x] Select light or dark theme for timer
  - [x] Counting time backwards
  - [x] Option to change application size



How to build installer
===
Installers are created with slightly modified [Squirrel.Windows](https://github.com/floatas/Squirrel.Windows)
1. Restore nuget packages.
``NuGet restore ..\Yapa.sln``
2. Run ``Build\BuildAndRelease.cmd`` 
Without arguments 2.0.0 version will be used.
To create specific version: ``Build\BuildAndRelease.cmd 2.4.9`` 
