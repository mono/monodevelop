
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

FILES =  \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.DeleteConfigDialog.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.NewConfigurationDialog.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.BaseDirectoryPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CombineBuildOptionsWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CombineConfigurationPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CombineEntryConfigurationsPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CombineInformationWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CommonAssemblySigningPreferences.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CustomCommandPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CustomCommandWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.GeneralProjectOptionsWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.OutputOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.RunOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.RuntimeOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.OptionPanels.StartupOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.Projects.Gui.Dialogs.RenameConfigDialog.cs \
	MonoDevelop.Projects.Gui.Completion/CompletionData.cs \
	MonoDevelop.Projects.Gui.Completion/CompletionDataList.cs \
	MonoDevelop.Projects.Gui.Completion/CompletionListWindow.cs \
	MonoDevelop.Projects.Gui.Completion/DeclarationViewWindow.cs \
	MonoDevelop.Projects.Gui.Completion/ICompletionData.cs \
	MonoDevelop.Projects.Gui.Completion/ICompletionWidget.cs \
	MonoDevelop.Projects.Gui.Completion/IParameterDataProvider.cs \
	MonoDevelop.Projects.Gui.Completion/ListWindow.cs \
	MonoDevelop.Projects.Gui.Completion/MethodParameterDataProvider.cs \
	MonoDevelop.Projects.Gui.Completion/ParameterInformationWindow.cs \
	MonoDevelop.Projects.Gui.Completion/ParameterInformationWindowManager.cs \
	MonoDevelop.Projects.Gui.Completion/TextUtilities.cs \
	MonoDevelop.Projects.Gui.Completion/TooltipWindow.cs \
	MonoDevelop.Projects.Gui.Completion/WindowTransparencyDecorator.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/ActiveLanguageCondition.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/BaseDirectoryPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/BaseDirectoryPanelWidget.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineBuildOptions.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineConfigurationPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CombineInformationPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CommonAssemblySigningPreferences.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CustomCommandPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CustomCommandPanelWidget.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/CustomCommandWidget.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/EnvVarList.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/GeneralProjectOptions.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/ItemTypeCondition.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/OutputOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/RunOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/RuntimeOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/SolutionItemConfigurationsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs.OptionPanels/StartupOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs/CombineOptionsDialog.cs \
	MonoDevelop.Projects.Gui.Dialogs/DeleteConfigDialog.cs \
	MonoDevelop.Projects.Gui.Dialogs/ItemOptionsDialog.cs \
	MonoDevelop.Projects.Gui.Dialogs/ItemOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs/MultiConfigItemOptionsDialog.cs \
	MonoDevelop.Projects.Gui.Dialogs/MultiConfigItemOptionsPanel.cs \
	MonoDevelop.Projects.Gui.Dialogs/NewConfigurationDialog.cs \
	MonoDevelop.Projects.Gui.Dialogs/ProjectOptionsDialog.cs \
	MonoDevelop.Projects.Gui.Dialogs/RenameConfigDialog.cs \
	MonoDevelop.Projects.Gui/IconCodon.cs \
	MonoDevelop.Projects.Gui/IconService.cs \
	MonoDevelop.Projects.Gui/ProjectsGuiServices.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.Projects.Gui.addin.xml

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
