
ADDIN_BUILD = $(top_builddir)/build/AddIns
ASSEMBLY = $(ADDIN_BUILD)/GnomePlatform.dll
REFS =  \
	$(GCONF_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../../build/bin/MonoDevelop.Core.dll \
	-r:../../../build/bin/MonoDevelop.Core.Gui.dll \
	-r:System

FILES = GnomePlatform.cs

RES = GnomePlatform.addin.xml

if ENABLE_GNOMEPLATFORM
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

if ENABLE_GNOMEPLATFORM
assemblydir = $(MD_ADDIN_DIR)/GnomePlatform
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
