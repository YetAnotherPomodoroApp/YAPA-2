$publish_key = $env:INSTALLER_PUBLISH_KEY
Get-ChildItem "Release" | Foreach-Object {
    $path =  $_.FullName

    .\curlUpload.bat $path $publish_key

} 