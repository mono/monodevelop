".nuget\NuGet.exe" restore Main.sln
"external\RefactoringEssentials\.nuget\NuGet.exe" restore external\RefactoringEssentials\RefactoringEssentials.sln
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" Main.sln /m /p:Configuration=ReleaseWin32 /p:Platform="Any CPU"
