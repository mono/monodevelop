
ADDIN_BUILD = $(top_builddir)/build/AddIns
ASSEMBLY = $(ADDIN_BUILD)/MacPlatform.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:System

FILES =  \
	AssemblyInfo.cs \
	Ige/IgeMacAttentionRequest.cs \
	Ige/IgeMacAttentionType.cs \
	Ige/IgeMacBundle.cs \
	Ige/IgeMacDock.cs \
	Ige/IgeMacMenu.cs \
	Ige/IgeMacMenuGroup.cs \
	Ige/ObjectManager.cs \
	MacPlatform.cs \
	Menus/OSXMenu.cs

RES = MacPlatform.addin.xml

if ENABLE_MACPLATFORM
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

if ENABLE_MACPLATFORM
assemblydir = $(MD_ADDIN_DIR)/MacPlatform
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
