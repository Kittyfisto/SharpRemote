@setlocal
@echo off

if not defined APPVEYOR_BUILD_VERSION (
	echo Error: Expected the environment variable APPVEYOR_BUILD_VERSION to be set, but it's not!
	exit /b -1
)

nuget pack SharpRemote\SharpRemote.nuspec -Version %APPVEYOR_BUILD_VERSION%-beta

endlocal
