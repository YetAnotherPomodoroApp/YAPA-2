set releaseDir=%1

set version=%2
if "%version%" == "" (
   set version=%BUILD_BUILDNUMBER%
)
squirrel\tools\squirrel --releasify YAPA2.%version%.nupkg --releaseDir=%releaseDir% --previous-releases-url=ftp://s1.floatas.net/YAPA-2 --no-msi