@echo off
setlocal

rem Find MSBuild from whichever Visual Studio (or Build Tools) is installed.
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo vswhere.exe not found. Install Visual Studio or the Visual Studio Build Tools.
    exit /b 1
)

set "MSBUILD="
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
    echo MSBuild not found in the installed Visual Studio.
    exit /b 1
)

rem -restore fetches the NuGet packages (PackageReference) before building.
"%MSBUILD%" "%~dp0..\YAPA.sln" -restore -p:Configuration=Release
exit /b %ERRORLEVEL%
