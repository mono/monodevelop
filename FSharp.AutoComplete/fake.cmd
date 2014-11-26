@echo off

pushd %~dp0

IF EXIST packages\FAKE\tools\Fake.exe GOTO FAKEINSTALLED

"..\lib\nuget\NuGet.exe" "install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion" "-Prerelease"

:FAKEINSTALLED

SET TARGET="All"

IF NOT [%1]==[] (set TARGET="%1")

"packages\FAKE\tools\Fake.exe" "build.fsx" "target=%TARGET%"

popd

