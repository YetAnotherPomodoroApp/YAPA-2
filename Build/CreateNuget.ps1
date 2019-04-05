 $fullVersion = (dir ..\Release\Yapa.exe).VersionInfo.FileVersion

 $squirrelVersionRegex = "\d+\.\d+\.\d+"
 $squirrelVersion = [regex]::matches($fullVersion, $squirrelVersionRegex)

 Write-Output "$squirrelVersion"

 #set nuget version
 (Get-Content -path Yapa2.nuspec -Raw) -replace '%version%',$squirrelVersion | Set-Content -Path Yapa2.nuspec

 ..\.nuget\nuget pack Yapa2.nuspec -version $squirrelVersion -Properties Configuration=Release


..\packages\squirrel.windows.1.5.28\tools\Squirrel --releasify YAPA2.$squirrelVersion.nupkg --releaseDir=Release --previous-releases-url=http://app.floatas.net/Installers/YAPA-2 --no-msi