".nuget\NuGet.exe" restore Main.sln
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" Main.sln /m /p:Configuration=DebugWin32 /p:Platform="Any CPU"
