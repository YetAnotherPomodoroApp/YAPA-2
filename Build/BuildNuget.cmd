set version=%1
if "%version%" == "" (
   set version=%BUILD_BUILDNUMBER%
)
..\.nuget\nuget pack Yapa2.nuspec -version %version% -Properties Configuration=Release