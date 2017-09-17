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

set prevReleaseUrl=%3
if "%prevReleaseUrl%" == "" (
   set prevReleaseUrl=ftp://s1.floatas.net/YAPA-2
)

..\packages\squirrel.windows.1.6.0\tools\Squirrel --releasify YAPA2.%version%.nupkg --releaseDir=%releaseDir% --previous-releases-url=%prevReleaseUrl% --no-msi  --framework-version=net47