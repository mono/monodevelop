ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/BooShell.dll
REFS =  \
	$(BOO_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.Setup.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll

RES =                                                         
 
FILES = BooShell.boo


if ENABLE_BOO
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(BOOC) $(REFS) $(build_resources:%=-embedres:%) $(build_sources) -o:$@ -t:library

if ENABLE_BOO
assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY)

endif

CLEANFILES = $(ASSEMBLY)
EXTRA_DIST = $(FILES) $(RES)
include $(top_srcdir)/Makefile.include
