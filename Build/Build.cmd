set version=%1
if "%version%" == "" (
   set version=%BUILD_BUILDNUMBER%
)
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

msbuild Build.proj /p:Configuration="Release" /p:build_number="%version%"
