BINDIR=$(abs_top_builddir)/build/bin
XBUILD=xbuild
XBUILD_ARGS=/property:Configuration=net_4_0_Debug /property:OutputPath=$(BINDIR)

all:
	$(XBUILD) src/symbols/mdb/Mono.Cecil.Mdb.csproj $(XBUILD_ARGS)

clean:
	$(XBUILD) src/symbols/mdb/Mono.Cecil.Mdb.csproj $(XBUILD_ARGS)

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
