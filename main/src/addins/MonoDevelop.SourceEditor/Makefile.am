DLLNAME = MonoDevelop.SourceEditor.dll
ASSEMBLY = $(top_builddir)/build/AddIns/$(DLLNAME)

FILES =  \
	gtk-gui/generated.cs \
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
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.SourceEditor.addin.xml \
	texteditoraddin.glade

GENERATED_FILES = \
AssemblyInfo.cs

DEPS =	\
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GCONF_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW2_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all: $(ASSEMBLY)

$(ASSEMBLY) : $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -t:library $(REFS)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

