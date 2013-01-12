#!/bin/bash

find . -name "*Runner.fsx" | xargs -L1 fsharpi
git status .
