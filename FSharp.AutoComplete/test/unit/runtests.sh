#!/bin/bash

cd `dirname $0`
NUGET="nuget/NuGet.exe"

if [[ ! -d "../../../lib/NUnit" ]];
then
    pushd ../../../lib
    mono $NUGET install nunit -Version 2.6.3 -ExcludeVersion
    popd
fi

if [[ ! -d "../../../lib/NUnit.Runners" ]];
then
    pushd ../../../lib
    mono $NUGET install nunit.runners -Version 2.6.3 -ExcludeVersion
    popd
fi

if [[ ! -d "../../../lib/FsUnit" ]];
then
    pushd ../../../lib
    mono $NUGET install fsunit -Version 1.3.0.1 -ExcludeVersion
    popd
fi

xbuild ProjectLoading/ProjectParserTests.fsproj
mono ../../../lib/NUnit.Runners/tools/nunit-console-x86.exe \
     ProjectLoading/bin/Debug/ProjectParserTests.dll

