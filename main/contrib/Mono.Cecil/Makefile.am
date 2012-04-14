BINDIR=$(abs_top_builddir)/build/bin
XBUILD=xbuild
XBUILD_ARGS=/property:Configuration=net_4_0_Debug /property:OutputPath=$(BINDIR)

all:
	$(XBUILD) src/symbols/mdb/Mono.Cecil.Mdb.csproj $(XBUILD_ARGS)

clean:
	$(XBUILD) src/symbols/mdb/Mono.Cecil.Mdb.csproj $(XBUILD_ARGS)

$(BINDIR)/Mono.Cecil.dll:
$(BINDIR)/Mono.Cecil.dll.mdb:
$(BINDIR)/Mono.Cecil.Mdb.dll:
$(BINDIR)/Mono.Cecil.Mdb.dll.mdb:

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = \
	$(BINDIR)/Mono.Cecil.dll \
	$(BINDIR)/Mono.Cecil.dll.mdb  \
	$(BINDIR)/Mono.Cecil.Mdb.dll \
	$(BINDIR)/Mono.Cecil.Mdb.dll.mdb

EXTRA_DIST = \
	$(wildcard src/*.cs)		\
	$(wildcard src/*.csproj)	\
	$(wildcard src/*/*.cs)		\
	$(wildcard src/*/*.csproj)	\
        $(wildcard src/*/*/*.cs)	\
	$(wildcard src/*/*/*.csproj)	\
        $(wildcard src/*/*/*/*.cs)	\
	$(wildcard src/*/*/*/*.csproj)	\
	$(wildcard src/*/*/*/*/*.cs)	\
	$(wildcard src/*/*/*/*/*.csproj)
