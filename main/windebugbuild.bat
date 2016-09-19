".nuget\NuGet.exe" restore Main.sln
"external\RefactoringEssentials\.nuget\NuGet.exe" restore external\RefactoringEssentials\RefactoringEssentials.sln
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" Main.sln /m /p:Configuration=DebugWin32 /p:Platform="Any CPU"

if DEFINED VS150COMNTOOLS (
	set VSPATH="%VS150COMNTOOLS%"
) else if DEFINED VS140COMNTOOLS (
	set VSPATH="%VS140COMNTOOLS%"
) else if DEFINED VS120COMNTOOLS (
	set VSPATH="%VS120COMNTOOLS%"
) else if DEFINED VS110COMNTOOLS (
	set VSPATH="%VS110COMNTOOLS%"
)
if exist %VSPATH%\..\..\VC\bin\editbin.exe (
	%VSPATH%\..\..\VC\bin\editbin.exe /largeaddressaware build\bin\MonoDevelop.exe
)