
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/Diff.dll

FILES = Diff.cs Merge.cs Patch.cs StructuredDiff.cs TextDiff.cs UnifiedDiff.cs

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(build_sources) -out:$@ -target:library

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

if ENABLE_VERSIONCONTROL
all: $(ASSEMBLY)
else
all:
endif

if ENABLE_VERSIONCONTROL
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(ASSEMBLY)
endif

include $(top_srcdir)/Makefile.include
