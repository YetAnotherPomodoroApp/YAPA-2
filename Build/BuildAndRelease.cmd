
set version=%1
if "%version%" == "" (
   set version=2.0.0
)

set releasePath=%2
if "%releasePath%" == "" (
   set releasePath=Releases
)

call Build.cmd %version%

call BuildNuget.cmd %version%

call Releasify.cmd %version%  %releasePath% 
