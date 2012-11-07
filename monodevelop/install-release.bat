@echo off
set MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%MSBUILD% MonoDevelop.FSharpBinding\MonoDevelop.FSharp.windows.fsproj /p:Configuration=Release
set MDROOT="%ProgramFiles(x86)%\MonoDevelop"
rmdir /s /q pack
mkdir pack\windows\Release
%MDROOT%\bin\mdtool.exe setup pack bin\Debug\FSharpBinding.windows.addin.xml -d:pack\windows\Release
%MDROOT%\bin\mdtool.exe setup install -y pack\windows\Release\MonoDevelop.FSharpBinding_3.2.1.mpack 
