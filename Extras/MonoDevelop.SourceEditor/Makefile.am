DLLNAME = MonoDevelop.SourceEditor.dll
CONFIG = $(DLLNAME).config
ASSEMBLY = $(top_builddir)/build/AddIns/$(DLLNAME)

FILES =  \
	MonoDevelop.SourceEditor.Actions/DefaultEditActions.cs \
	MonoDevelop.SourceEditor.Actions/EditActionCollection.cs \
	MonoDevelop.SourceEditor.Actions/IEditAction.cs \
	MonoDevelop.SourceEditor.Codons/EditActionCodon.cs \
	MonoDevelop.SourceEditor.FormattingStrategy/DefaultFormattingStrategy.cs \
	MonoDevelop.SourceEditor.FormattingStrategy/IFormattingStrategy.cs \
	MonoDevelop.SourceEditor.Gui.Dialogs/EditTemplateGroupDialog.cs \
	MonoDevelop.SourceEditor.Gui.Dialogs/GotoLineNumberDialog.cs \
	MonoDevelop.SourceEditor.Gui.OptionPanels/BehaviorTextEditorPanel.cs \
	MonoDevelop.SourceEditor.Gui.OptionPanels/GeneralTextEditorPanel.cs \
	MonoDevelop.SourceEditor.Gui.OptionPanels/MarkersTextEditorPanel.cs \
	MonoDevelop.SourceEditor.Gui.OptionPanels/SyntaxHighlightingPanel.cs \
	MonoDevelop.SourceEditor.Gui/LanguageItemWindow.cs \
	MonoDevelop.SourceEditor.Gui/SourceEditorBuffer.cs \
	MonoDevelop.SourceEditor.Gui/SourceEditorDisplayBinding.cs \
	MonoDevelop.SourceEditor.Gui/SourceEditorView.cs \
	MonoDevelop.SourceEditor.Gui/SourceEditorWidget.cs \
	MonoDevelop.SourceEditor.Gui/TextNavigationPoint.cs \
	MonoDevelop.SourceEditor/EditorCommands.cs \
	MonoDevelop.SourceEditor/Services.cs \
	MonoDevelop.SourceEditor/SourceViewService.cs

RES =  \
	bookmark.png \
	EditorBindings.glade \
	texteditoraddin.glade

GENERATED_FILES = \
AssemblyInfo.cs

REFS =  \
	$(GCONF_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS) \
	$(MONO_CAIRO_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

ADDIN = MonoDevelop.SourceEditor.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all : $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY) : $(build_sources) $(build_resources)
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -t:library $(REFS) \
	&& cp $(CONFIG) $(top_builddir)/build/AddIns/.

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(CONFIG)

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = $(CONFIG)

EXTRA_DIST = $(FILES) $(CONFIG).in $(RES) $(ADDIN)

include $(top_srcdir)/Makefile.include

