@echo off
set MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%MSBUILD% ..\FSharp.CompilerBinding\FSharp.CompilerBinding.fsproj /p:Configuration=Release
%MSBUILD% MonoDevelop.FSharpBinding\MonoDevelop.FSharp.windows.fsproj /p:Configuration=Release
set MDROOT="%ProgramFiles(x86)%\Xamarin Studio"
rmdir /s /q pack
mkdir pack\windows\Release
xcopy /s /I /y dependencies\AspNetMvc4 bin\windows\Release\packages\AspNetMvc4
%MDROOT%\bin\mdtool.exe setup pack bin\windows\Release\FSharpBinding.dll -d:pack\windows\Release
%MDROOT%\bin\mdtool.exe setup install -y pack\windows\Release\MonoDevelop.FSharpBinding_3.2.19.mpack 
