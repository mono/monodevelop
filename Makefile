# Makefile for compiling and installing F# AutoComplete engine

TARGETS = bin/FSharp.CompilerBinding.dll bin/fsautocomplete.exe
FSHARP_COMPILER_EDITOR = monodevelop/MonoDevelop.FSharpBinding/packages/FSharp.Compiler.Editor.1.0.7/lib/net40/FSharp.Compiler.Editor.dll


all: $(TARGETS)

$(FSHARP_COMPILER_EDITOR):
	mozroots --import --sync --quiet
	(cd monodevelop/MonoDevelop.FSharpBinding && mono ../../lib/nuget/NuGet.exe install -OutputDirectory packages)

bin/fsautocomplete.exe: $(FSHARP_COMPILER_EDITOR)
	(cd FSharp.AutoComplete && xbuild FSharp.AutoComplete.fsproj)

bin/FSharp.CompilerBinding.dll: $(FSHARP_COMPILER_EDITOR)
	(cd FSharp.CompilerBinding && xbuild FSharp.CompilerBinding.fsproj)

clean:
	-rm -fr bin

autocomplete: bin/fsautocomplete.exe
