FILES = \
AssemblyInfo.cs \
MonoCombine.cs \
MonoMakefile.cs \
MonoMakefileFormat.cs \
MonoProject.cs \
Commands.cs \
MonoProjectConfiguration.cs \
MonoTestProvider.cs

REFS = /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
       /r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
       /r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
       /r:$(top_builddir)/build/AddIns/NUnit/MonoDevelop.NUnit.dll \
       /r:nunit.core.dll \
       $(GTK_SHARP_LIBS) \
       $(GLADE_SHARP_LIBS) \
       $(GCONF_SHARP_LIBS) \
       $(GTKSOURCEVIEW_SHARP_LIBS)

ADDIN = MonoDeveloperExtensions.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDeveloperExtensions

DLL = $(ADDIN_BUILD)/MonoDeveloperExtensions.dll

if ENABLE_MONOEXTENSIONS
all: $(ADDIN_BUILD)/$(ADDIN) $(DLL)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(DLL): $(build_sources) $(srcdir)/$(GLADEFILE)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS)

CLEANFILES = $(DLL) $(DLL).mdb $(ADDIN_BUILD)/$(ADDIN)

EXTRA_DIST = $(FILES) $(ADDIN)

include $(top_srcdir)/Makefile.include

if ENABLE_MONOEXTENSIONS
MonoDeveloperExtensionsdir = $(MD_ADDIN_DIR)/MonoDeveloperExtensions
MonoDeveloperExtensions_DATA = $(DLL) $(ADDIN_BUILD)/$(ADDIN)
endif
