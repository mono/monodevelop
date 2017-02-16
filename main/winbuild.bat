git submodule update --init --recursive
"external\nuget-binary\NuGet.exe" restore Main.sln
"external\nuget-binary\NuGet.exe" restore external\RefactoringEssentials\RefactoringEssentials.sln
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" Main.sln /m /p:Configuration=DebugWin32 /p:Platform="Any CPU" %*

if NOT [%ERRORLEVEL%] == ["0"] (
	for %%x in (%CMDCMDLINE%) do if /i "%%~x" == "/c" pause
	exit /b %ERRORLEVEL%
)
