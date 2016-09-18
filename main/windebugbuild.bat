".nuget\NuGet.exe" restore Main.sln
"external\RefactoringEssentials\.nuget\NuGet.exe" restore external\RefactoringEssentials\RefactoringEssentials.sln
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" Main.sln /m /p:Configuration=DebugWin32 /p:Platform="Any CPU"

if exist "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\bin\editbin.exe" (
	"C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\bin\editbin.exe" /largeaddressaware build/bin/MonoDevelop.exe
)