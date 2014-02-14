#!/bin/bash

cd `dirname $0`
NUGET="nuget/NuGet.exe"

if [[ ! -d "../../../lib/NUnit.2.6.1" ]];
then
    pushd ../../../lib
    mono $NUGET install nunit -Version 2.6.1
    popd
fi

if [[ ! -d "../../../lib/NUnit.Runners.2.6.1" ]];
then
    pushd ../../../lib
    mono $NUGET install nunit.runners -Version 2.6.1
    popd
fi

if [[ ! -d "../../../lib/FsUnit.1.1.1.0" ]];
then
    pushd ../../../lib
    mono $NUGET install fsunit -Version 1.1.1.0
    popd
fi

xbuild ProjectLoading/ProjectParserTests.fsproj
mono ../../../lib/NUnit.Runners.2.6.1/tools/nunit-console-x86.exe \
     ProjectLoading/bin/Debug/ProjectParserTests.dll

