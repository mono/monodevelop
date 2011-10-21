@echo off
set MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%MSBUILD% src\FSharp.MonoDevelop.sln
set MDROOT="%ProgramFiles(x86)%\MonoDevelop"
copy bin\FSharpBinding.* %MDROOT%\AddIns\BackendBindings
