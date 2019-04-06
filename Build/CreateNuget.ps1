 #-------Get Version from YAPA.exe
 $fullVersion = (dir ..\Release\Yapa.exe).VersionInfo.FileVersion
 $baseUri = 'http://app.floatas.net/installers/YAPA-2/'
 $squirrelVersionRegex = "\d+\.\d+\.\d+"
 $squirrelVersion = [regex]::matches($fullVersion, $squirrelVersionRegex)

 Write-Output "$squirrelVersion"

 #-------Set version in Yapa2.nuspec
 (Get-Content -path Yapa2.nuspec -Raw) -replace '%version%',$squirrelVersion | Set-Content -Path Yapa2.nuspec

 ..\.nuget\nuget pack Yapa2.nuspec -version $squirrelVersion -Properties Configuration=Release

 #--------------Get RELEASES file and latest release for deltas
Invoke-WebRequest -Uri "$($baseUri)RELEASES" -OutFile "Release\RELEASES"
$releases = Get-Content "Release\RELEASES"

$releaseItems = $releases.Split([Environment]::NewLine)

$index = $releaseItems.Count - 1
$lastRelease = ""
while ($true) {
    $lastRelease = $releaseItems[$index]
    $releaseParts = $lastRelease.Split(" ")

    if ($releaseParts[1] -match "full\.nupkg") {
        $lastRelease = $releaseParts[1]
        break;
    }
    else {
        $index--;
    }
}

Write-Output $lastRelease
$lastReleasePath = $baseUri+ $lastRelease
Invoke-WebRequest -Uri  $lastReleasePath -OutFile "Release\$lastRelease"


..\packages\squirrel.windows.1.5.28\tools\Squirrel --releasify YAPA2.$squirrelVersion.nupkg --releaseDir=Release  --no-msi








