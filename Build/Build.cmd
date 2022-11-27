pushd "%~dp0"
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
popd
msbuild ..\YAPA.sln /p:Configuration="Release"
