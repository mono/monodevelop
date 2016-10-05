# Makefile for compiling, installing and packing F# MonoDevelop plugin on Mono
#
#   run 'make' to compile the plugin against the installed version of MonoDevelop detected by ./configure.sh
#   run 'make install' to compile and install the plugin against the installed version of MonoDevelop detected by ./configure.sh
#   run 'make pack-all' to create a deployment binary packages for the known set of supported MonoDevelop versions

VERSION=6.0.0

MDTOOL = mono '../../build/bin/mdtool.exe'

# (MDVERSION4) can be set to something like (3.0.4, 3.0.4.7) to compile
# against the dependencies/... binaries for a specific version of MonoDevelop. This allows
# us to prepare new editions of the binding for several different versions of MonoDevelop.
MDVERSION4=6.1

MDROOT=../../build


# The default configuration is Release since Roslyn
ifeq ($(config),)
config=Release
endif

.PHONY: all

all: build

build: MonoDevelop.FSharpBinding/MonoDevelop.FSharp.fsproj MonoDevelop.FSharpBinding/FSharpBinding.addin.xml
	(xbuild MonoDevelop.FSharp.sln /p:Configuration=$(config))

pack: build
	-rm -fr pack/$(config)
	@-mkdir -p pack/$(config)
	$(MDTOOL) setup pack ../../../main/build/AddIns/BackendBindings/FSharpBinding.dll -d:pack/$(config)

install: pack
	$(MDTOOL) setup install -y pack/$(config)/MonoDevelop.FSharpBinding_$(MDVERSION4).mpack 

uninstall:
	$(MDTOOL) setup uninstall MonoDevelop.FSharpBinding

release: 
	$(MAKE) config=Release pack

clean:
	-rm -fr bin
	-rm -fr pack
	-rm -fr MonoDevelop.FSharpBinding/MonoDevelop.FSharp.*.fsproj
	-rm -fr MonoDevelop.FSharpBinding/obj
	(cd MonoDevelop.FSharp.Gui && xbuild MonoDevelop.FSharp.Gui.csproj /target:Clean)

