# Makefile for compiling and installing F# AutoComplete engine

TARGETS = bin/FSharp.CompilerBinding.dll bin/fsautocomplete.exe
FSHARP_COMPILER_SERVICE = packages/FSharp.Compiler.Service.0.0.4-alpha/lib/net40/FSharp.Compiler.Service.dll


all: $(TARGETS)

$(FSHARP_COMPILER_SERVICE):
	mozroots --import --sync --quiet
	cd FSharp.AutoComplete && mono ../lib/nuget/NuGet.exe install -OutputDirectory ../packages

bin/fsautocomplete.exe: $(FSHARP_COMPILER_SERVICE)
	(cd FSharp.AutoComplete && xbuild FSharp.AutoComplete.fsproj)

bin/FSharp.CompilerBinding.dll: $(FSHARP_COMPILER_SERVICE)
	(cd FSharp.CompilerBinding && xbuild FSharp.CompilerBinding.fsproj)

clean:
	-rm -fr FSharp.AutoComplete/bin
	-rm -fr FSharp.CompilerBinding/bin

autocomplete: bin/fsautocomplete.exe
