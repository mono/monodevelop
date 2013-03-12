#!/bin/bash

cd `dirname $0`
find . -name "*Runner.fsx" | xargs -L1 -P1 -t fsharpi --exec
find . -name "*.txt" | xargs perl -p -i -e "s/\/.*FSharp.AutoComplete\/test\/(.*)/<absolute path removed>\/test\/\$1/"
git status .
