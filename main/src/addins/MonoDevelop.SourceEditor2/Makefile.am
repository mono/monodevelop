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
	gtk-gui/MonoDevelop.SourceEditor.SearchAndReplaceWidget.cs \
	gtk-gui/MonoDevelop.SourceEditor.SearchWidget.cs \
	gtk-gui/MonoDevelop.SourceEditor.SourceEditorWidget.cs \
	MonoDevelop.SourceEditor.OptionPanels/BehaviorPanel.cs \
	MonoDevelop.SourceEditor.OptionPanels/GeneralOptionsPanel.cs \
	MonoDevelop.SourceEditor.OptionPanels/HighlightingPanel.cs \
	MonoDevelop.SourceEditor.OptionPanels/MarkerPanel.cs \
	MonoDevelop.SourceEditor/ClassQuickFinder.cs \
	MonoDevelop.SourceEditor/CompileErrorTooltipProvider.cs \
	MonoDevelop.SourceEditor/DebugValueTooltipProvider.cs \
	MonoDevelop.SourceEditor/DebugValueWindow.cs \
	MonoDevelop.SourceEditor/EditActions.cs \
	MonoDevelop.SourceEditor/ExtendibleTextEditor.cs \
	MonoDevelop.SourceEditor/GotoLineNumberWidget.cs \
	MonoDevelop.SourceEditor/HighlightPropertiesSemanticRule.cs \
	MonoDevelop.SourceEditor/LanguageItemTooltipProvider.cs \
	MonoDevelop.SourceEditor/LanguageItemWindow.cs \
	MonoDevelop.SourceEditor/MarkerOperationsHandler.cs \
	MonoDevelop.SourceEditor/SearchAndReplaceWidget.cs \
	MonoDevelop.SourceEditor/SearchWidget.cs \
	MonoDevelop.SourceEditor/SourceEditorCommands.cs \
	MonoDevelop.SourceEditor/SourceEditorDisplayBinding.cs \
	MonoDevelop.SourceEditor/SourceEditorOptions.cs \
	MonoDevelop.SourceEditor/SourceEditorView.cs \
	MonoDevelop.SourceEditor/SourceEditorWidget.cs 

GENERATED_FILES = 

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.SourceEditor.addin.xml  

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:../../../build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	-r:../../../build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:../../../build/bin/Mono.Debugging.dll \
	-r:../../../build/bin/Mono.TextEditor.dll \
	-r:../../../build/bin/MonoDevelop.Components.dll \
	-r:../../../build/bin/MonoDevelop.Core.dll \
	-r:../../../build/bin/MonoDevelop.Core.Gui.dll \
	-r:../../../build/bin/MonoDevelop.Ide.dll \
	-r:../../../build/bin/MonoDevelop.Projects.dll \
	-r:../../../build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all: $(ASSEMBLY)

$(ASSEMBLY) : $(build_sources) $(build_resources)
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -t:library $(REFS)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

