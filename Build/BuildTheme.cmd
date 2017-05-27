set ThemeRepository=%1
set ProjectToBuild=%2
set OutputDirectory=%3
set ThemeDirectory=%4

IF EXIST %ThemeDirectory% (
   rd %ThemeDirectory% /s /Q
)

git clone %ThemeRepository% --recurse-submodules

git pull %ThemeRepository%

cd %ThemeDirectory%

git submodule update --remote --merge

call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

msbuild %ProjectToBuild% /p:Configuration="Release" /p:OutputPath="%OutputDirectory%"
