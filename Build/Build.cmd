pushd "%~dp0"
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"
popd
msbuild ..\YAPA.sln /p:Configuration="Release"
