ADDIN_BUILD = $(top_builddir)/build/AddIns/ChangeLogAddIn
ASSEMBLY = $(ADDIN_BUILD)/ChangeLogAddIn.dll

REFS =	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	$(GECKO_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
	ChangeLogAddIn.cs \
	ChangeLogAddInOptionsPanel.cs \
	AssemblyInfo.cs

ADDIN = ChangeLogAddIn.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) 

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/ChangeLogAddIn
assembly_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN)

include $(top_srcdir)/Makefile.include
