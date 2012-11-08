@echo off
set MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%MSBUILD% MonoDevelop.FSharpBinding\MonoDevelop.FSharp.windows.fsproj /p:Configuration=Debug
set MDROOT="%ProgramFiles(x86)%\MonoDevelop"
rmdir /s /q pack
mkdir pack\windows\Debug
%MDROOT%\bin\mdtool.exe setup pack bin\windows\Debug\FSharpBinding.windows.addin.xml -d:pack\windows\Debug
%MDROOT%\bin\mdtool.exe setup install -y pack\windows\Debug\MonoDevelop.FSharpBinding_3.2.2.mpack 
