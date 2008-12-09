
ADDIN_BUILD = $(top_builddir)/build/AddIns
ASSEMBLY = $(ADDIN_BUILD)/GnomePlatform.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll

REFS =  \
	$(GCONF_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:System

FILES =  \
	AssemblyInfo.cs \
	Gio.cs \
	GnomePlatform.cs

RES = GnomePlatform.addin.xml

if ENABLE_GNOMEPLATFORM
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

dll_config = GnomePlatform.dll.config
build_dll_config = $(ADDIN_BUILD)/$(dll_config)

$(build_dll_config): $(srcdir)/$(dll_config)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(dll_config) $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

if ENABLE_GNOMEPLATFORM
assemblydir = $(MD_ADDIN_DIR)/GnomePlatform
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(build_dll_config)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(build_dll_config)
EXTRA_DIST = $(FILES) $(RES) $(dll_config)

include $(top_srcdir)/Makefile.include
