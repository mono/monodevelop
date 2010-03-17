DLLNAME = MonoDevelop.HexEditor.dll
ASSEMBLY = $(top_builddir)/build/AddIns/$(DLLNAME)

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	Mono.MHex.Data/Buffer.cs \
	Mono.MHex.Data/Caret.cs \
	Mono.MHex.Data/EditMode.cs \
	Mono.MHex.Data/HexEditorData.cs \
	Mono.MHex.Data/ISegment.cs \
	Mono.MHex.Data/PieceTable.cs \
	Mono.MHex.Data/RedBlackTree.cs \
	Mono.MHex.Data/ReplaceEventArgs.cs \
	Mono.MHex.Data/Segment.cs \
	Mono.MHex.Data/Selection.cs \
	Mono.MHex.Data/UpdateRequest.cs \
	Mono.MHex.Rendering/DashedLineMargin.cs \
	Mono.MHex.Rendering/EmptySpaceMargin.cs \
	Mono.MHex.Rendering/GutterMargin.cs \
	Mono.MHex.Rendering/HexEditorMargin.cs \
	Mono.MHex.Rendering/HexEditorStyle.cs \
	Mono.MHex.Rendering/IconMargin.cs \
	Mono.MHex.Rendering/Margin.cs \
	Mono.MHex.Rendering/TextEditorMargin.cs \
	Mono.MHex/BookmarkActions.cs \
	Mono.MHex/CaretMoveActions.cs \
	Mono.MHex/DeleteActions.cs \
	Mono.MHex/HexEditor.cs \
	Mono.MHex/HexEditorOptions.cs \
	Mono.MHex/IHexEditorOptions.cs \
	Mono.MHex/MiscActions.cs \
	Mono.MHex/ScrollActions.cs \
	Mono.MHex/SelectionActions.cs \
	Mono.MHex/SimpleEditMode.cs \
	MonoDevelop.HexEditor/DisplayBinding.cs \
	MonoDevelop.HexEditor/HexEditorNodeExtension.cs \
	MonoDevelop.HexEditor/HexEditorView.cs \
	MonoDevelop.HexEditor/MonoDevelopHexEditorStyle.cs 

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.HexEditor.addin.xml  

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.AssemblyBrowser.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/NRefactory.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core

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

