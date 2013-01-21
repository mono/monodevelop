#!/bin/bash

cd `dirname $0`
find . -name "*Runner.fsx" | xargs -L1 -P8 -t fsharpi --exec
find . -name "*.txt" | xargs perl -p -i -e "s/File '.*\/test\/(.*)'/File 'test\/\$1'/"
git status .
