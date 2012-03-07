BINDIR=$(top_builddir)/build/bin
XBUILD=xbuild
XBUILD_ARGS=/property:Configuration=net_4_0_Debug /property:OutputPath=$(realpath $(BINDIR)) /property:IntermediateOutputPath=$(realpath .)/obj/

all:
	$(XBUILD) src/symbols/mdb/Mono.Cecil.Mdb.csproj $(XBUILD_ARGS)

clean:
	$(XBUILD) src/symbols/mdb/Mono.Cecil.Mdb.csproj $(XBUILD_ARGS)

