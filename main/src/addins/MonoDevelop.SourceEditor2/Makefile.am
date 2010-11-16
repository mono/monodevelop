DLLNAME = MonoDevelop.SourceEditor2.dll
ASSEMBLY = $(top_builddir)/build/AddIns/$(DLLNAME)

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.SourceEditor.GotoLineNumberWidget.cs \
	gtk-gui/MonoDevelop.SourceEditor.OptionPanels.BehaviorPanel.cs \
	gtk-gui/MonoDevelop.SourceEditor.OptionPanels.GeneralOptionsPanel.cs \
	gtk-gui/MonoDevelop.SourceEditor.OptionPanels.HighlightingPanel.cs \
	gtk-gui/MonoDevelop.SourceEditor.OptionPanels.MarkerPanel.cs \
	gtk-gui/MonoDevelop.SourceEditor.PrintSettingsWidget.cs \
	gtk-gui/MonoDevelop.SourceEditor.SearchAndReplaceWidget.cs \
	MonoDevelop.SourceEditor.Extension/TemplateCodon.cs \
	MonoDevelop.SourceEditor.Extension/TemplateExtensionNodeLoader.cs \
	MonoDevelop.SourceEditor.OptionPanels/BehaviorPanel.cs \
	MonoDevelop.SourceEditor.OptionPanels/GeneralOptionsPanel.cs \
	MonoDevelop.SourceEditor.OptionPanels/HighlightingPanel.cs \
	MonoDevelop.SourceEditor.OptionPanels/MarkerPanel.cs \
	MonoDevelop.SourceEditor/AutoSave.cs \
	MonoDevelop.SourceEditor/CompileErrorTooltipProvider.cs \
	MonoDevelop.SourceEditor/Counters.cs \
	MonoDevelop.SourceEditor/DebugValueTooltipProvider.cs \
	MonoDevelop.SourceEditor/DebugValueWindow.cs \
	MonoDevelop.SourceEditor/DefaultFormatter.cs \
	MonoDevelop.SourceEditor/DynamicAbbrevHandler.cs \
	MonoDevelop.SourceEditor/EditActions.cs \
	MonoDevelop.SourceEditor/ExtensibleTextEditor.cs \
	MonoDevelop.SourceEditor/GotoLineNumberWidget.cs \
	MonoDevelop.SourceEditor/IdeViMode.cs \
	MonoDevelop.SourceEditor/ISourceEditorOptions.cs \
	MonoDevelop.SourceEditor/LanguageItemTooltipProvider.cs \
	MonoDevelop.SourceEditor/LanguageItemWindow.cs \
	MonoDevelop.SourceEditor/MarkerOperationsHandler.cs \
	MonoDevelop.SourceEditor/MessageBubbleCommands.cs \
	MonoDevelop.SourceEditor/MessageBubbleHighlightPopupWindow.cs \
	MonoDevelop.SourceEditor/MessageBubbleTextMarker.cs \
	MonoDevelop.SourceEditor/PinnedWatchWidget.cs \
	MonoDevelop.SourceEditor/PrintSettingsWidget.cs \
	MonoDevelop.SourceEditor/SearchAndReplaceWidget.cs \
	MonoDevelop.SourceEditor/SourceEditorCommands.cs \
	MonoDevelop.SourceEditor/SourceEditorDisplayBinding.cs \
	MonoDevelop.SourceEditor/SourceEditorOptions.cs \
	MonoDevelop.SourceEditor/SourceEditorPrintOperation.cs \
	MonoDevelop.SourceEditor/SourceEditorView.cs \
	MonoDevelop.SourceEditor/SourceEditorWidget.cs \
	MonoDevelop.SourceEditor/StatusBox.cs \
	MonoDevelop.SourceEditor/StyledSourceEditorOptions.cs \
	MonoDevelop.SourceEditor/SyntaxModeCodon.cs \
	MonoDevelop.SourceEditor/SyntaxModeService.cs 

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.SourceEditor.addin.xml \
	MonoDevelop.SourceEditor/searchoptions.png


DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/NRefactory.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY) : $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -t:library $(REFS) $(build_deps)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

