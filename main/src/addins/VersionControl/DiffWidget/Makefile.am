
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/DiffWidget.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/VersionControl/Diff.dll \
	-r:Mono.Posix
FILES = widget.cs

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(top_builddir)/build/AddIns/VersionControl/Diff.dll
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

all: $(ASSEMBLY) $(ASSEMBLY).mdb

addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(top_srcdir)/Makefile.include
