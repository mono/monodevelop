
ASSEMBLY = $(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/contrib/Mono.Addins.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CustomCommandPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CustomCommandWidget.cs \
	MonoDevelop.Projects.Gui.Completion/CodeCompletionData.cs \
	MonoDevelop.Projects.Gui.Completion/CodeCompletionDataProvider.cs \
	MonoDevelop.Projects.Gui.Completion/CommentCompletionDataProvider.cs \
	MonoDevelop.Projects.Gui.Completion/CompletionListWindow.cs \
	MonoDevelop.Projects.Gui.Completion/DeclarationViewWindow.cs \
	MonoDevelop.Projects.Gui.Completion/ICompletionData.cs \
	MonoDevelop.Projects.Gui.Completion/ICompletionDataProvider.cs \
	MonoDevelop.Projects.Gui.Completion/ICompletionWidget.cs \
	MonoDevelop.Projects.Gui.Completion/IParameterDataProvider.cs \
	MonoDevelop.Projects.Gui.Completion/ListWindow.cs \
	MonoDevelop.Projects.Gui.Completion/MethodParameterDataProvider.cs \
	MonoDevelop.Projects.Gui.Completion/ParameterInformationWindow.cs \
	MonoDevelop.Projects.Gui.Completion/ParameterInformationWindowManager.cs \
	MonoDevelop.Projects.Gui.Completion/TextUtilities.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineBuildOptions.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineConfigurationPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineEntryConfigurationsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineInformationPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineStartupPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CompileFileProjectOptions.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CustomCommandPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CustomCommandPanelWidget.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CustomCommandWidget.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/GeneralProjectOptions.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/OutputOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/RuntimeOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs/CombineOptionsDialog.cs \
	MonoDevelop.Projects.Gui.Dialogs/ConfigurationManager.cs \
	MonoDevelop.Projects.Gui.Dialogs/ProjectOptionsDialog.cs \
	MonoDevelop.Projects.Gui/IconService.cs \
	MonoDevelop.Projects.Gui/ProjectsGuiServices.cs

RES =  \
	Base.glade \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml

GENERATED_FILES = \
AssemblyInfo.cs

ADDIN = MonoDevelop.Projects.Gui.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_resources:%=/resource:%) $(build_sources)

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include
