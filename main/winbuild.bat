set filename=%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe

if not exist filename (
	filename=%WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
)

"%filename%" Main.sln /m /p:Configuration=DebugWin32 /p:Platform="Any CPU"
