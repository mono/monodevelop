SETLOCAL
SET MONODEVELOP_DEV_ADDINS=%~dp0bin
CD ..\..\build
CALL bin\MonoDevelop.exe --no-redirect
