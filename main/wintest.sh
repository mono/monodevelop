if [ $# -lt 1 ]; then
	build/bin/mdtool run-md-tests -labels build/tests/UnitTests.dll
	build/bin/mdtool run-md-tests -labels build/tests/*.Tests.dll
	if [ ! -f "external/nrefactory/bin/Debug/ICSharpCode.NRefactory.Tests.dll" ]; then
		build/bin/mdtool run-md-tests -labels external/nrefactory/bin/Debug/ICSharpCode.NRefactory.Tests.dll
	else
		build/bin/mdtool run-md-tests -labels external/nrefactory/bin/Release/ICSharpCode.NRefactory.Tests.dll
	fi
	if [ ! -f "external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Debug/MonoDevelop.FSharp.Tests.dll" ]; then
		build/bin/mdtool run-md-tests -labels external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Debug/MonoDevelop.FSharp.Tests.dll
	else
		build/bin/mdtool run-md-tests -labels external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Release/MonoDevelop.FSharp.Tests.dll
	fi
else
	for arg in $@; do
		if [[ $arg == *ICSharpCode.NRefactory.Tests.dll ]]; then
			arg="external/nrefactory/bin/Debug/ICSharpCode.NRefactory.Tests.dll"
			if [ ! -f "$arg" ]; then
				arg="external/nrefactory/bin/Release/ICSharpCode.NRefactory.Tests.dll"
			fi
		elif [[ $arg == *MonoDevelop.FSharp.Tests.dll ]]; then
			arg="external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Debug/MonoDevelop.FSharp.Tests.dll"
			if [ ! -f "$arg" ]; then
				arg="external/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Release/MonoDevelop.FSharp.Tests.dll"
			fi
		elif [[ $arg != build/tests/* ]]; then
			arg="build/tests/$arg"
		fi

		if [ -f $arg ]; then
			(build/bin/mdtool run-md-tests -labels $arg) || exit $?
		fi
	done
fi
