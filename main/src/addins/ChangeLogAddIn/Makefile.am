ADDIN_BUILD = $(top_builddir)/build/AddIns/ChangeLogAddIn
ASSEMBLY = $(ADDIN_BUILD)/ChangeLogAddIn.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:Mono.Posix

FILES =  \
	AddLogEntryDialog.cs \
	AssemblyInfo.cs \
	ChangeLogAddIn.cs \
	ChangeLogAddInOptionPanelWidget.cs \
	ChangeLogAddInOptionsPanel.cs \
	ChangeLogData.cs \
	ChangeLogService.cs \
	CommitDialogExtensionWidget.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.ChangeLogAddIn.AddLogEntryDialog.cs \
	gtk-gui/MonoDevelop.ChangeLogAddIn.ChangeLogAddInOptionPanelWidget.cs \
	gtk-gui/MonoDevelop.ChangeLogAddIn.ProjectOptionPanelWidget.cs \
	ProjectOptionPanel.cs \
	ProjectOptionPanelWidget.cs

RES =  \
	ChangeLogAddIn.addin.xml \
	gtk-gui/gui.stetic

if ENABLE_VERSIONCONTROL
all: $(ASSEMBLY) 
else
  all:
endif

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

if ENABLE_VERSIONCONTROL
assemblydir = $(MD_ADDIN_DIR)/ChangeLogAddIn
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
