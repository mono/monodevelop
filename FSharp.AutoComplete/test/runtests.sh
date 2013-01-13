#!/bin/bash

find . -name "*Runner.fsx" | xargs -L1 -P8 -t fsharpi
git status .
