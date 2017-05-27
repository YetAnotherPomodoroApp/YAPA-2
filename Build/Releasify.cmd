set version=%1
if "%version%" == "" (
   set version=%BUILD_BUILDNUMBER%
)
if "%version%" == "" (
   set version="2.0.0"
)

set releaseDir=%2
if "%releaseDir%" == "" (
   set releaseDir=Releases
)


..\packages\squirrel.windows.1.5.28\tools\Squirrel --releasify YAPA2.%version%.nupkg --releaseDir=%releaseDir% --previous-releases-url=ftp://s1.floatas.net/YAPA-2 --no-msi