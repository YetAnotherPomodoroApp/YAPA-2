set ThemeRepository=%1
set ProjectToBuild=%2
set OutputDirectory=%3

git clone %ThemeRepository% --recurse-submodules

git pull %ThemeRepository%

git submodule update --remote --merge

call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

msbuild %ProjectToBuild% /p:Configuration="Release" /p:OutputPath="%OutputDirectory%"
