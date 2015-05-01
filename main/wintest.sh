if [ $# -lt 1 ]; then
	build/bin/mdtool run-md-tests build/tests/UnitTests.dll
	build/bin/mdtool run-md-tests build/tests/*.Tests.dll
	build/bin/mdtool run-md-tests external/nrefactory/bin/Debug/ICSharpCode.NRefactory.Tests.dll
else
	for arg in $@; do
		if [[ $arg == *ICSharpCode.NRefactory.Tests.dll ]]; then
			arg="external/nrefactory/bin/Debug/ICSharpCode.NRefactory.Tests.dll"
		elif [[ $arg != build/tests/* ]]; then
			arg="build/tests/$arg"
		fi

		if [ -f $arg ]; then
			(build/bin/mdtool run-md-tests $arg) || exit $?
		fi
	done
fi
