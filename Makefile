# Makefile for compiling and installing F# AutoComplete engine

.PHONY: all clean unit-test integration-test test

all: ~/.config/.mono/certs
	xbuild FSharp.AutoComplete.sln

~/.config/.mono/certs:
	mozroots --import --sync --quiet

clean:
	-rm -fr FSharp.AutoComplete/bin
	-rm -fr FSharp.CompilerBinding/bin
	-rm -fr packages

unit-test: all
	FSharp.AutoComplete/test/unit/runtests.sh

integration-test: all
	FSharp.AutoComplete/test/integration/runtests.sh

test: unit-test integration-test
