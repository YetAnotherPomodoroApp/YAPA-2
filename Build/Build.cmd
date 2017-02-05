set config=%1
if "%config%" == "" (
   set config=debug
)
set version=%2
if "%version%" == "" (
   set version=2.0.0.0
)
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

msbuild Build.proj /p:Configuration="%config%" /p:build_number="%version%"
