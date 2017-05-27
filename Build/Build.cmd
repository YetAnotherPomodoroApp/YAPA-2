set version=%1
if "%version%" == "" (
   set version=%BUILD_BUILDNUMBER%
)
if "%version%" == "" (
   set version="2.0.0"
)
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

msbuild Build.proj /p:Configuration="Release" /p:build_number="%version%"
