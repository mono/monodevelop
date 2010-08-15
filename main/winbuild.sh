export PATH="/c/Windows/Microsoft.NET/Framework/v3.5:$PATH"
MSBuild.exe Main.sln -p:Configuration=DebugWin32 -p:Platform=x86
