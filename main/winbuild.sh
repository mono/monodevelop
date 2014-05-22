export PATH="/c/Windows/Microsoft.NET/Framework/v4.0.30319:$PATH"
pushd ..; git submodule update --init --recursive || exit 1; popd
MSBuild.exe Main.sln -p:Configuration=DebugWin32 $*
