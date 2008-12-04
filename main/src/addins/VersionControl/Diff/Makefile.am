
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/Diff.dll

REFS = -r:System.Xml

FILES =  \
	Diff.cs \
	Merge.cs \
	Patch.cs \
	StructuredDiff.cs \
	TextDiff.cs \
	UnifiedDiff.cs

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

if ENABLE_VERSIONCONTROL
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

if ENABLE_VERSIONCONTROL
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

include $(top_srcdir)/Makefile.include
