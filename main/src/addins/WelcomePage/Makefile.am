
ADDIN_BUILD = $(top_builddir)/build/AddIns/WelcomePage
ASSEMBLY = $(ADDIN_BUILD)/WelcomePage.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.WelcomePage.WelcomePageFallbackWidget.cs \
	WelcomePageCommands.cs \
	WelcomePageFallbackWidget.cs \
	WelcomePageOptionPanel.cs \
	WelcomePageView.cs \
	XslGettextCatalog.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	WelcomePage.addin.xml \
	WelcomePage.xsl \
	WelcomePageContent.xml
 
DATA_FILES =  \
	mono-bg.png \
	mono-decoration.png \
	mono-logo.png \
	WelcomePage.css


DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(notdir $@) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/WelcomePage
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
