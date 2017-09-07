@echo off
setlocal enableextensions enabledelayedexpansion

rem try to find MSBuild in VS2017
rem the "correct" way is to use a COM API. not easy here.

FOR %%E in (Enterprise, Professional, Community) DO (
	set "MSBUILD_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\%%E\MSBuild\15.0\Bin\MSBuild.exe"
	if exist "!MSBUILD_EXE!" goto :build
)

REM Couldn't be located in the standard locations, expand search
FOR /F "delims=" %%E IN ('dir /b /ad "%ProgramFiles(x86)%\Microsoft Visual Studio\"') DO (
	set "MSBUILD_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\%%E\MSBuild\15.0\Bin\MSBuild.exe"
	if exist "!MSBUILD_EXE!" goto :build

	FOR /F "delims=" %%F IN ('dir /b /ad "%ProgramFiles(x86)%\Microsoft Visual Studio\%%E"') DO (
		set "MSBUILD_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\%%E\%%F\MSBuild\15.0\Bin\MSBuild.exe"
		if exist "!MSBUILD_EXE!" goto :build
	)
)

echo Could not find MSBuild 15
exit /b 1

:build

git submodule sync || goto :error
git submodule update --init --recursive || goto :error
"external\nuget-binary\NuGet.exe" restore Main.sln || goto :error

"%MSBUILD_EXE%" external\fsharpbinding\.paket\paket.targets /t:RestorePackages /p:PaketReferences="%~dp0external\fsharpbinding\MonoDevelop.FSharpBinding\paket.references"

set "CONFIG=DebugWin32"
set "PLATFORM=Any CPU"

rem only perform integrated restore on RefactoringEssentials, it fails on the whole solution
"%MSBUILD_EXE%" external\RefactoringEssentials\RefactoringEssentials.2017.sln /target:Restore %* || goto :error

"%MSBUILD_EXE%" Main.sln /bl:MonoDevelop.binlog /m "/p:Configuration=%CONFIG%" "/p:Platform=%PLATFORM%" %* || goto :error
goto :eof

:error

for %%x in (%CMDCMDLINE%) do if /i "%%~x" == "/c" pause
exit /b %ERRORLEVEL%
