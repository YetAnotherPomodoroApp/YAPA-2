$publish_key = $env:INSTALLER_PUBLISH_KEY

function Upload([string]$path, [string]$name) {
    & .\curlUpload.bat $path $publish_key $name
    if ($LASTEXITCODE -ne 0) { throw "the upload of $(Split-Path -Leaf $path) failed (curl exit code $LASTEXITCODE)" }
}

#RELEASES last: users' update checks read it, so the packages it names must be on the server first.
#If anything fails before it, users stay on the previous version.
$files = Get-ChildItem "Release" -File | Sort-Object { $_.Name -eq "RELEASES" }, Name
foreach ($file in $files) {
    Write-Output "uploading $($file.Name)"
    Upload $file.FullName
}

#The website's download link (yapa2.app) is YAPA2.exe: make it this version's Setup
$setup = Get-ChildItem "Release" -Filter "YAPA2.*.exe" | Select-Object -First 1
Write-Output "uploading $($setup.Name) as YAPA2.exe (the website's download)"
Upload $setup.FullName "YAPA2.exe"
