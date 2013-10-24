if [ $# -lt 1 ]; then
	build/bin/mdtool run-md-tests build/tests/UnitTests.dll
	build/bin/mdtool run-md-tests build/tests/*.Tests.dll
else
	for arg in $@; do
		if [[ $arg != build/tests/* ]]; then
			arg="build/tests/$arg"
		fi
		build/bin/mdtool run-md-tests $arg
	done
fi
