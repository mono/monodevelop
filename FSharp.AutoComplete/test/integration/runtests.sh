#!/bin/bash

cd `dirname $0`
find . -name "*Runner.fsx" | xargs -L1 -P1 -t fsharpi --exec
find . -name "*.txt"  -o -name "*.json" | xargs perl -p -i -e "s/\/.*?FSharp.AutoComplete\/test\/(.*?(\"|\$))/<absolute path removed>\/test\/\$1/g"

if [ "true" == "$TRAVIS" ]
then
    git diff --exit-code .
else
    git status .
    git diff --quiet .
fi

