#!/bin/bash

cd `dirname $0`

xbuild ProjectLoading/ProjectParserTests.fsproj /property:OutputPath="../build"
find build -name "*Tests.dll" | xargs mono ../../packages/NUnit.Runners.2.6.3/tools/nunit-console-x86.exe -result="build/TestResult.xml"

