ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/BooShell.dll

REFS =	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	$(GTK_SHARP_LIBS) \
	$(BOO_LIBS)

RES =

FILES = BooShell.boo

if ENABLE_BOO
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(BOOC) $(REFS) $(RES:%=-embedres:%) $(build_sources) -o:$@ -t:library

if ENABLE_BOO
assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY)

endif

CLEANFILES = $(ASSEMBLY)
EXTRA_DIST = $(FILES) $(RES)
include $(top_srcdir)/Makefile.include
