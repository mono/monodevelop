export PATH="/c/Windows/Microsoft.NET/Framework/v4.0.30319:$PATH"
pushd ..; git submodule init; git submodule update; popd
MSBuild.exe Main.sln -p:Configuration=DebugWin32 -p:Platform=x86 $*
