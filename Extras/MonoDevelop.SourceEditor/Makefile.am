DLLNAME = MonoDevelop.SourceEditor.dll
CONFIG = $(DLLNAME).config
DLL = $(top_builddir)/build/AddIns/$(DLLNAME)

FILES = \
MonoDevelop.SourceEditor/SourceViewService.cs \
MonoDevelop.SourceEditor.Properties/TextEditorProperties.cs \
MonoDevelop.SourceEditor.InsightWindow/IInsightDataProvider.cs \
MonoDevelop.SourceEditor.InsightWindow/IndexerInsightDataProvider.cs \
MonoDevelop.SourceEditor.InsightWindow/InsightWindow.cs \
MonoDevelop.SourceEditor.InsightWindow/MethodInsightDataProvider.cs \
MonoDevelop.SourceEditor.Gui/SourceEditorBuffer.cs \
MonoDevelop.SourceEditor.Gui/SourceEditorDisplayBinding.cs \
MonoDevelop.SourceEditor.Gui/SourceEditorView.cs \
MonoDevelop.SourceEditor.Gui/SourceEditorWidget.cs \
MonoDevelop.SourceEditor.Gui.OptionPanels/BehaviorTextEditorPanel.cs \
MonoDevelop.SourceEditor.Gui.OptionPanels/CodeTemplatePanel.cs \
MonoDevelop.SourceEditor.Gui.OptionPanels/GeneralTextEditorPanel.cs \
MonoDevelop.SourceEditor.Gui.OptionPanels/MarkersTextEditorPanel.cs \
MonoDevelop.SourceEditor.Gui.OptionPanels/SyntaxHighlightingPanel.cs \
MonoDevelop.SourceEditor.Document/DefaultFormattingStrategy.cs \
MonoDevelop.SourceEditor.FormattingStrategy/IFormattableDocument.cs \
MonoDevelop.SourceEditor.FormattingStrategy/IFormattingStrategy.cs \
MonoDevelop.SourceEditor.FormattingStrategy/IndentStyle.cs \
MonoDevelop.SourceEditor.Gui.Dialogs/EditTemplateGroupDialog.cs \
MonoDevelop.SourceEditor.Gui.Dialogs/GotoLineNumberDialog.cs \
MonoDevelop.SourceEditor/EditorCommands.cs \
MonoDevelop.SourceEditor.Actions/IEditAction.cs \
MonoDevelop.SourceEditor.Codons/EditActionCodon.cs \
MonoDevelop.SourceEditor/Services.cs

RES = \
texteditoraddin.glade \
EditorBindings.glade

GENERATED_FILES = \
AssemblyInfo.cs

REFS = /r:System.Drawing.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	/r:$(top_builddir)/build/AddIns/ICSharpCode.SharpRefactory.dll \
	$(GTK_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GCONF_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS)

ADDIN = MonoDevelop.SourceEditor.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all : $(DLL) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(DLL) : $(build_sources) $(RES)
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(RES:%=/resource:%) $(build_sources) -out:$@ -t:library $(REFS) \
	&& cp $(CONFIG) $(top_builddir)/build/AddIns/.

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(DLL) $(CONFIG)

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

CLEANFILES = $(DLL) $(DLL).mdb
DISTCLEANFILES = $(CONFIG)

EXTRA_DIST = $(FILES) $(CONFIG).in $(RES) $(ADDIN)

include $(top_srcdir)/Makefile.include

