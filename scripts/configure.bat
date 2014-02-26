@echo off
set DIR=%~dp0\
if not exist %DIR%\configure.exe (
    csc /nologo /out:%DIR%\configure.exe %DIR%\configure.cs
)
%DIR%\configure.exe %*