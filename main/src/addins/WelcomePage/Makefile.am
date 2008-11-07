
ADDIN_BUILD = $(top_builddir)/build/AddIns/WelcomePage
ASSEMBLY = $(ADDIN_BUILD)/WelcomePage.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.WelcomePage.WelcomePageFallbackWidget.cs \
	WelcomePageBrowser.cs \
	WelcomePageCommands.cs \
	WelcomePageFallbackWidget.cs \
	WelcomePageOptionPanel.cs \
	WelcomePageView.cs

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

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(notdir $@) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/WelcomePage
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
