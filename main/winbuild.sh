export PATH="/c/Program Files (x86)/MSBuild/14.0/Bin:$PATH"
pushd ..; git submodule update --init --recursive || exit 1; popd
./.nuget/NuGet.exe restore Main.sln
MSBuild.exe -m Main.sln -p:Configuration=DebugWin32 $*
