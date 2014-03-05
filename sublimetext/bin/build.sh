#!/bin/bash

# Copied from: http://stackoverflow.com/a/246128
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

pushd "$DIR/../FSharp"

python builder.py
case `uname` in
  'Linux')
    cp -f ./dist/FSharp.sublime-package ~/.config/sublime-text-3/Installed\ Packages
    ;;
  'Darwin')
    cp -f ./dist/FSharp.sublime-package ~/Library/Application\ Support/Sublime\ Text\ 3/Installed\ Packages
    ;;
esac

popd
