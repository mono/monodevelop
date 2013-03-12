#!/bin/bash

cd `dirname $0`
NUGET=`type -p NuGet.exe`

if [[ ! -d "../../../lib/NUnit.2.6.1" ]];
then
    type "NuGet.exe" >/dev/null 2>&1 || { echo >&2 "NuGet.exe not found on PATH. Aborting"; exit 1; }
    pushd ../../../lib
    mono $NUGET install nunit -Version 2.6.1
    popd
fi

if [[ ! -d "../../../lib/NUnit.Runners.2.6.1" ]];
then
    type "NuGet.exe" >/dev/null 2>&1 || { echo >&2 "NuGet.exe not found on PATH. Aborting"; exit 1; }
    pushd ../../../lib
    mono $NUGET install nunit.runners -Version 2.6.1
    popd
fi

if [[ ! -d "../../../lib/FsUnit.1.1.1.0" ]];
then
    type "NuGet.exe" >/dev/null 2>&1 || { echo >&2 "NuGet.exe not found on PATH. Aborting"; exit 1; }
    pushd ../../../lib
    mono $NUGET install fsunit -Version 1.1.1.0
    popd
fi

xbuild ProjectLoadingFsUnit/ProjectParserTests.fsproj
mono ../../../lib/NUnit.Runners.2.6.1/tools/nunit-console-x86.exe \
     ProjectLoadingFsUnit/bin/Debug/ProjectParserTests.dll

