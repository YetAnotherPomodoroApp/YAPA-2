set ThemeRepository=%1
set ProjectToBuild=%2
set ThemeDirectory=%3

IF EXIST %ThemeDirectory% (
   rd %ThemeDirectory% /s /Q
)

git clone %ThemeRepository% 

git pull %ThemeRepository%

call ".nuget\NuGet.exe" restore %ThemeDirectory%\\%ProjectToBuild%
call ".nuget\NuGet.exe" update %ThemeDirectory%\\%ProjectToBuild% -Id YAPA.WPF.Shared
call ".nuget\NuGet.exe" update %ThemeDirectory%\\%ProjectToBuild% -Id YAPA.WPF


cd %ThemeDirectory%


call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

msbuild %ProjectToBuild% /p:Configuration="Release"
