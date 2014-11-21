# Makefile for compiling and installing F# AutoComplete engine

.PHONY: all clean integration-test test

all: ~/.config/.mono/certs
	./fake build

~/.config/.mono/certs:
	mozroots --import --sync --quiet

clean:
	-rm -fr bin
	-rm -fr ../FSharp.CompilerBinding/bin
	-rm -fr packages
	-rm -rf test/unit/build

integration-test: all
	./fake integrationtest

test: integration-test
