./.nuget/NuGet.exe restore Main.sln
export PATH="/c/Program Files (x86)/MSBuild/14.0/Bin:$PATH"
pushd ..; git submodule update --init --recursive || exit 1; popd
MSBuild.exe Main.sln -p:Configuration=DebugWin32 $*
