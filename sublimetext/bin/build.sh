#!/bin/bash

# Copied from: http://stackoverflow.com/a/246128
__FILE_DIR__="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Copies FSharp.sublime-package to Packages/Installed Packages.
publish_package () {
    cp -f dist/FSharp.sublime-package "$data_path/Installed Packages"
}

# Copies FSharp's tests to Packags/FSharp_Tests.
publish_tests () {
	mkdir -p "$data_path/Packages/FSharp_Tests"
	pushd "$__FILE_DIR__/../FSharp_Tests"
		# Coppy support files.
		cp -f test_runner.py "$data_path/Packages/FSharp_Tests"
		cp -f FSharpTests.sublime-commands "$data_path/Packages/FSharp_Tests"
		pushd tests
			# Copy actual Python packages containing tests.
			cp * -r "$data_path/Packages/FSharp_Tests"
		popd
	popd
}

pushd "$__FILE_DIR__/../FSharp"
    mkdir -p dist
	# Build FSharp.sublime-package.
	python builder.py
	case `uname` in
	  'Linux')
	    data_path=~/.config/sublime-text-3
	    publish_package
	    publish_tests
	    ;;
	  'Darwin')
	    data_path =~/Library/Application\ Support/Sublime\ Text \3
	    publish_tests
	    ;;
	esac
popd
