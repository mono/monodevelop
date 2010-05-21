BINDIR=$(top_builddir)/build/bin

all: $(CECIL) $(CECIL_MDB) $(CECIL_DECOMPILER)

CECIL_DLL=Mono.Cecil.dll
CECIL=$(BINDIR)/$(CECIL_DLL)
CECIL_DEBUG=$(BINDIR)/$(CECIL_DLL).mdb
CECIL_DIR=$(srcdir)/Mono.Cecil
CECIL_CSFILES=$(addprefix $(CECIL_DIR)/, $(shell sed -f $(srcdir)/stripnewline.sed  $(CECIL_DIR)/Mono.Cecil.dll.sources))
CECIL_SNK=$(srcdir)/cecil.snk
CECIL_DIST= $(CECIL_CSFILES) $(CECIL_SNK)

$(CECIL): $(CECIL_CSFILES)
	mkdir -p $(BINDIR)
	mcs $(CECIL_CSFILES) /keyfile:$(CECIL_SNK) /debug /target:library /out:$(CECIL)

$(CECIL_DEBUG): $(CECIL)

CECIL_MDB_DLL=Mono.Cecil.Mdb.dll
CECIL_MDB=$(BINDIR)/$(CECIL_MDB_DLL)
CECIL_MDB_DEBUG=$(BINDIR)/$(CECIL_MDB_DLL).mdb
CECIL_MDB_DIR=$(srcdir)/Mono.Cecil.Mdb
CECIL_MDB_CSFILES=$(addprefix $(CECIL_MDB_DIR)/, $(shell sed -f $(srcdir)/stripnewline.sed  $(CECIL_MDB_DIR)/Mono.Cecil.Mdb.dll.sources))
CECIL_MDB_DIST= $(CECIL_MDB_CSFILES) $(CECIL_SNK)

$(CECIL_MDB): $(CECIL_MDB_CSFILES) $(CECIL)
	mkdir -p $(BINDIR)
	gmcs $(CECIL_MDB_CSFILES) /debug /target:library /keyfile:$(CECIL_SNK) /r:$(CECIL) /d:CECIL /out:$(CECIL_MDB)

$(CECIL_MDB_DEBUG): $(CECIL_MDB)

CECIL_DECOMPILER_DLL=Cecil.Decompiler.dll
CECIL_DECOMPILER=$(BINDIR)/$(CECIL_DECOMPILER_DLL)
CECIL_DECOMPILER_DEBUG=$(BINDIR)/$(CECIL_DECOMPILER_DLL).mdb
CECIL_DECOMPILER_DIR=$(srcdir)/Cecil.Decompiler
CECIL_DECOMPILER_CSFILES=$(CECIL_DECOMPILER_DIR)/*/*.cs
CECIL_DECOMPILER_DIST= $(CECIL_DECOMPILER_CSFILES)

$(CECIL_DECOMPILER): $(CECIL_DECOMPILER_CSFILES) $(CECIL)
	mkdir -p $(BINDIR)
	gmcs /debug /target:library /r:$(CECIL) /r:System /r:System.Core /r:System.Xml /out:$(CECIL_DECOMPILER) $(CECIL_DECOMPILER_CSFILES)

## Helpers for re-importing the latest source
## Can specify revision with SVN_REVISION environment variable and username with $USER

SVN_SERVER=svn+ssh://$(USER)@mono-cvs.ximian.com
SVN_R_REVISION=$(addprefix -r, $(SVN_REVISION))

update-source-Mono.Cecil:
	svn rm Mono.Cecil && \
	svn cp $(SVN_R_REVISION) $(SVN_SERVER)/source/trunk/mcs/class/Mono.Cecil .

update-source-Mono.Cecil.Mdb:
	svn rm Mono.Cecil.Mdb && \
	svn cp $(SVN_R_REVISION) $(SVN_SERVER)/source/trunk/mcs/class/Mono.Cecil.Mdb .

update-source-Mono.CompilerServices.SymbolWriter:
	svn rm Mono.CompilerServices.SymbolWriter && \
	svn cp $(SVN_R_REVISION) $(SVN_SERVER)/source/trunk/mcs/class/Mono.CompilerServices.SymbolWriter .

update-source: update-source-Mono.Cecil update-source-Mono.Cecil.Mdb update-source-Mono.CompilerServices.SymbolWriter


assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(CECIL) $(CECIL_MDB) $(CECIL_DEBUG) $(CECIL_MDB_DEBUG) $(CECIL_DECOMPILER) $(DECIL_DECOMPILER_DEBUG)

CLEANFILES =  $(CECIL) $(CECIL_MDB) $(CECIL_DEBUG) $(CECIL_MDB_DEBUG) $(CECIL_DECOMPILER) $(DECIL_DECOMPILER_DEBUG)
EXTRA_DIST = \
	$(CECIL_DIST) $(CECIL_DIR)/Mono.Cecil.dll.sources \
	$(CECIL_MDB_DIST) $(CECIL_MDB_DIR)/Mono.Cecil.Mdb.dll.sources \
	stripnewline.sed \
	$(CECIL_DECOMPILER_DIST)

