@echo off
rem Uploads one file: curlUpload.bat <file> <key> [name on the server]
rem HTTPS, so the key is not sent in the clear; -f turns a refusal (wrong key, server error) into a failure
if "%~3"=="" (
    curl.exe -f -sS -o NUL -F "files=@%~1" "https://app.floatas.net/installers/upload?key=%~2&application=YAPA-2"
) else (
    curl.exe -f -sS -o NUL -F "files=@%~1;filename=%~3" "https://app.floatas.net/installers/upload?key=%~2&application=YAPA-2"
)
exit /b %ERRORLEVEL%
