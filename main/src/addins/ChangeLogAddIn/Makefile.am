ADDIN_BUILD = $(top_builddir)/build/AddIns/ChangeLogAddIn
ASSEMBLY = $(ADDIN_BUILD)/ChangeLogAddIn.dll

DEPS =  \
	$(top_builddir)/build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix

FILES =  \
	AddLogEntryDialog.cs \
	AssemblyInfo.cs \
	ChangeLogAddIn.cs \
	ChangeLogPolicy.cs \
	ChangeLogService.cs \
	CommitDialogExtensionWidget.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.ChangeLogAddIn.AddLogEntryDialog.cs \
	gtk-gui/MonoDevelop.ChangeLogAddIn.ProjectOptionPanelWidget.cs \
	OldChangeLogData.cs \
	ProjectOptionPanel.cs \
	ProjectOptionPanelWidget.cs

RES =  \
	ChangeLogAddIn.addin.xml \
	gtk-gui/gui.stetic \
	Policies.xml

if ENABLE_VERSIONCONTROL
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
  all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

if ENABLE_VERSIONCONTROL
assemblydir = $(MD_ADDIN_DIR)/ChangeLogAddIn
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
