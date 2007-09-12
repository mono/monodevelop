BINDIR=$(top_builddir)/build/bin

all: $(CECIL)

CECIL_DLL=Mono.Cecil.dll
CECIL = $(BINDIR)/$(CECIL_DLL)
CECIL_DIR=$(srcdir)/src
CECIL_CSFILES=$(addprefix $(CECIL_DIR)/, $(shell sed -f $(srcdir)/stripnewline.sed  $(CECIL_DIR)/Mono.Cecil.dll.sources))
CECIL_DIST= $(CECIL_CSFILES)

$(CECIL): $(CECIL_CSFILES)
	mkdir -p $(BINDIR)
	mcs $(CECIL_CSFILES) /target:library /out:$(CECIL)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(CECIL)

CLEANFILES =  $(CECIL)
EXTRA_DIST = $(CECIL_DIST) $(CECIL_DIR)/Mono.Cecil.dll.sources stripnewline.sed
