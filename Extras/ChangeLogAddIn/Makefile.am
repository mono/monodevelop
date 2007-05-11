ADDIN_BUILD = $(top_builddir)/build/AddIns/ChangeLogAddIn
ASSEMBLY = $(ADDIN_BUILD)/ChangeLogAddIn.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/VersionControl/VersionControlAddIn.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:Mono.Posix

FILES =  \
	AddLogEntryDialog.cs \
	AssemblyInfo.cs \
	ChangeLogAddIn.cs \
	ChangeLogAddInOptionPanelWidget.cs \
	ChangeLogAddInOptionsPanel.cs \
	CommitDialogExtensionWidget.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.ChangeLogAddIn.AddLogEntryDialog.cs \
	gtk-gui/MonoDevelop.ChangeLogAddIn.ChangeLogAddInOptionPanelWidget.cs

ADDIN = ChangeLogAddIn.addin.xml

if ENABLE_VERSIONCONTROL
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) 
else
  all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_sources) -out:$@ -target:library

if ENABLE_VERSIONCONTROL
assemblydir = $(MD_ADDIN_DIR)/ChangeLogAddIn
assembly_DATA = $(ASSEMBLY) $(ADDIN)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN)

include $(top_srcdir)/Makefile.include
