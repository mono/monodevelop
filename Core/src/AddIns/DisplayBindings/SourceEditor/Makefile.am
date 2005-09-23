DLLNAME = MonoDevelop.SourceEditor.dll
CONFIG = $(DLLNAME).config
DLL = $(top_builddir)/build/bin/$(DLLNAME)

FILES = \
Gui/SourceEditorWidget.cs \
Gui/SourceEditorBuffer.cs \
Gui/SourceEditorView.cs \
Gui/SourceEditorDisplayBinding.cs \
Gui/OptionPanels/BehaviorTextEditorPanel.cs \
Gui/OptionPanels/MarkersTextEditorPanel.cs \
Gui/OptionPanels/CodeTemplatePanel.cs \
Gui/OptionPanels/GeneralTextEditorPanel.cs \
Gui/OptionPanels/SyntaxHighlightingPanel.cs \
Gui/Dialogs/EditTemplateGroupDialog.cs \
Gui/Dialogs/GotoLineNumberDialog.cs \
InsightWindow/IndexerInsightDataProvider.cs \
InsightWindow/MethodInsightDataProvider.cs \
InsightWindow/InsightWindow.cs \
InsightWindow/IInsightDataProvider.cs \
Commands/EditorCommands.cs \
Commands/IEditAction.cs \
Properties/TextEditorProperties.cs \
FormattingStrategy/DefaultFormattingStrategy.cs \
FormattingStrategy/IndentStyle.cs \
FormattingStrategy/IFormattingStrategy.cs \
FormattingStrategy/IFormattableDocument.cs \
Codons/EditActionCodon.cs \
Services/SourceViewService.cs

GENERATED_FILES = \
AssemblyInfo.cs

REFS = /r:System.Drawing.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Utils.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
	/r:$(top_builddir)/build/bin/ICSharpCode.SharpRefactory.dll \
	$(GTK_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GCONF_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS)

ADDIN = MonoDevelopEditor.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all : $(DLL) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(DLL) : $(build_sources) $(srcdir)/EditorBindings.glade $(srcdir)/texteditoraddin.glade
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(build_sources) -out:$@ -t:library $(REFS) \
	-resource:$(srcdir)/EditorBindings.glade,EditorBindings.glade \
	-resource:$(srcdir)/texteditoraddin.glade,texteditoraddin.glade \
	&& cp $(CONFIG) $(top_builddir)/build/bin/.

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(DLL) $(CONFIG)

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

CLEANFILES = $(DLL) $(DLL).mdb
DISTCLEANFILES = $(CONFIG)

EXTRA_DIST = $(FILES) $(CONFIG).in EditorBindings.glade texteditoraddin.glade $(ADDIN)

include $(top_srcdir)/Makefile.include

