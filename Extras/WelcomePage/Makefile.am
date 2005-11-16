
ADDIN_BUILD = $(top_builddir)/build/AddIns/WelcomePage
ASSEMBLY = $(ADDIN_BUILD)/WelcomePage.dll

DLLS = /r:nunit.framework.dll \
	/r:nunit.core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	$(GECKO_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
	WelcomePageCommands.cs \
	WelcomePageOptionPanel.cs \
	WelcomePageView.cs

RES =

DATA_FILES = \
	mono-bg.png \
	mono-logo.png \
	WelcomePage.css \
	WelcomePage.xsl

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

ADDIN = WelcomePage.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(DATA_FILE_BUILD): $(srcdir)/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(notdir $@) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(DLLS) $(RES:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/WelcomePage
assembly_DATA = $(ASSEMBLY) $(ADDIN) $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD) $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
