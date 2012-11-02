@echo off
set MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%MSBUILD% MonoDevelop.FSharpBinding\MonoDevelop.FSharp.fsproj /p:Configuration=Release
set MDROOT="%ProgramFiles(x86)%\MonoDevelop"
rmdir /s /q pack
mkdir pack
%MDROOT%\bin\mdtool.exe setup pack bin\Release\FSharpBinding.addin.xml -d:pack
%MDROOT%\bin\mdtool.exe setup install -y pack\MonoDevelop.FSharpBinding_3.2.1.mpack 
