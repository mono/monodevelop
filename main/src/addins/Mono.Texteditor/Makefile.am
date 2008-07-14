
ADDIN_BUILD = $(top_builddir)/build/bin
ASSEMBLY = $(top_builddir)/build/bin/Mono.TextEditor.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	Mono.TextEditor.Highlighting/Chunk.cs \
	Mono.TextEditor.Highlighting/ChunkStyle.cs \
	Mono.TextEditor.Highlighting/DefaultStyle.cs \
	Mono.TextEditor.Highlighting/IXmlProvider.cs \
	Mono.TextEditor.Highlighting/Keywords.cs \
	Mono.TextEditor.Highlighting/Marker.cs \
	Mono.TextEditor.Highlighting/Match.cs \
	Mono.TextEditor.Highlighting/Rule.cs \
	Mono.TextEditor.Highlighting/SemanticRule.cs \
	Mono.TextEditor.Highlighting/Span.cs \
	Mono.TextEditor.Highlighting/Style.cs \
	Mono.TextEditor.Highlighting/SyntaxMode.cs \
	Mono.TextEditor.Highlighting/SyntaxModeService.cs \
	Mono.TextEditor.Highlighting/WorkerThread.cs \
	Mono.TextEditor.Highlighting/XmlReadHelper.cs \
	Mono.TextEditor/BookmarkMargin.cs \
	Mono.TextEditor/Caret.cs \
	Mono.TextEditor/CodeSegmentPreviewWindow.cs \
	Mono.TextEditor/DefaultEditActions.cs \
	Mono.TextEditor/Document.cs \
	Mono.TextEditor/DocumentLocation.cs \
	Mono.TextEditor/DocumentUpdateRequest.cs \
	Mono.TextEditor/EditAction.cs \
	Mono.TextEditor/FoldingType.cs \
	Mono.TextEditor/FoldMarkerMargin.cs \
	Mono.TextEditor/FoldSegment.cs \
	Mono.TextEditor/GapBuffer.cs \
	Mono.TextEditor/GutterMargin.cs \
	Mono.TextEditor/IBuffer.cs \
	Mono.TextEditor/IMargin.cs \
	Mono.TextEditor/ISearchEngine.cs \
	Mono.TextEditor/ISegment.cs \
	Mono.TextEditor/ITooltipProvider.cs \
	Mono.TextEditor/LineEventArgs.cs \
	Mono.TextEditor/LineSegment.cs \
	Mono.TextEditor/LineSegmentTree.cs \
	Mono.TextEditor/LineSplitter.cs \
	Mono.TextEditor/RedBlackTree.cs \
	Mono.TextEditor/ReplaceEventArgs.cs \
	Mono.TextEditor/SearchResult.cs \
	Mono.TextEditor/Segment.cs \
	Mono.TextEditor/TextEditor.cs \
	Mono.TextEditor/TextEditorData.cs \
	Mono.TextEditor/TextEditorOptions.cs \
	Mono.TextEditor/TextMarker.cs \
	Mono.TextEditor/TextViewMargin.cs 
	Mono.TextEditor.Highlighting/Match.cs \
	Mono.TextEditor.Highlighting/Style.cs \
	Mono.TextEditor.Highlighting/SyntaxMode.cs \
	Mono.TextEditor.Highlighting/XmlReadHelper.cs \
	Mono.TextEditor.Highlighting/ChunkStyle.cs \
	Mono.TextEditor.Highlighting/SyntaxModeService.cs \
	Mono.TextEditor.Highlighting/Span.cs \
	Mono.TextEditor.Tests/Mono.TextEditor.Tests.DefaultEditActions/InsertNewLineTests.cs \
	Mono.TextEditor.Tests/Mono.TextEditor.Tests.DefaultEditActions/CaretMoveTests.cs

RES =  \
	AspNetSyntaxMode.xml \
	BooSyntaxMode.xml \
	BrownStyle.xml \
	C64Style.xml \
	CPPSyntaxMode.xml \
	CSharpSyntaxMode.xml \
	CSyntaxMode.xml \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	JavascriptSyntaxMode.xml \
	JavaSyntaxMode.xml \
	OblivionStyle.xml \
	PythonSyntaxMode.xml \
	TangoLightStyle.xml \
	ValaSyntaxMode.xml \
	VBNetSyntaxMode.xml \
	VisualStudioStyle.xml \
	XmlSyntaxMode.xml

all: $(ASSEMBLY)

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
