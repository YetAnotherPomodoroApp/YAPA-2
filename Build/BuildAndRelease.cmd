set config=%1
if "%config%" == "" (
   set config=debug
)
set version=%2
if "%version%" == "" (
   set version=2.0.0
)
set releasePath=%3
if "%releasePath" == "" (
   set releasePath=Releases\
)

call Build.cmd %config% %version%

call BuildNuget.cmd %version%

call Releasify.cmd %version% %releasePath%
