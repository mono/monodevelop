
ADDIN_BUILD = $(top_builddir)/build/bin
ASSEMBLY = $(top_builddir)/build/bin/Mono.TextEditor.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Xml

CONFIG=Mono.TextEditor.dll.config

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	Mono.TextEditor.Highlighting.Regex/Regex.cs \
	Mono.TextEditor.Highlighting.Regex/RegexMatch.cs \
	Mono.TextEditor.Highlighting/Chunk.cs \
	Mono.TextEditor.Highlighting/ChunkStyle.cs \
	Mono.TextEditor.Highlighting/DefaultStyle.cs \
	Mono.TextEditor.Highlighting/IXmlProvider.cs \
	Mono.TextEditor.Highlighting/JaySyntaxMode.cs \
	Mono.TextEditor.Highlighting/Keywords.cs \
	Mono.TextEditor.Highlighting/Marker.cs \
	Mono.TextEditor.Highlighting/MarkupSyntaxMode.cs \
	Mono.TextEditor.Highlighting/Match.cs \
	Mono.TextEditor.Highlighting/ReferencedChunkStyle.cs \
	Mono.TextEditor.Highlighting/Rule.cs \
	Mono.TextEditor.Highlighting/SemanticRule.cs \
	Mono.TextEditor.Highlighting/Span.cs \
	Mono.TextEditor.Highlighting/Style.cs \
	Mono.TextEditor.Highlighting/SyntaxMode.cs \
	Mono.TextEditor.Highlighting/SyntaxModeService.cs \
	Mono.TextEditor.Highlighting/WorkerThread.cs \
	Mono.TextEditor.Highlighting/XmlReadHelper.cs \
	Mono.TextEditor.PopupWindow/IListDataProvider.cs \
	Mono.TextEditor.PopupWindow/ListWidget.cs \
	Mono.TextEditor.PopupWindow/ListWindow.cs \
	Mono.TextEditor.PopupWindow/TooltipWindow.cs \
	Mono.TextEditor.Theatrics/Actor.cs \
	Mono.TextEditor.Theatrics/AnimatedBox.cs \
	Mono.TextEditor.Theatrics/AnimatedVBox.cs \
	Mono.TextEditor.Theatrics/AnimatedWidget.cs \
	Mono.TextEditor.Theatrics/Animation.cs \
	Mono.TextEditor.Theatrics/BounceFadePopupWindow.cs \
	Mono.TextEditor.Theatrics/Choreographer.cs \
	Mono.TextEditor.Theatrics/Pulsator.cs \
	Mono.TextEditor.Theatrics/SingleActorStage.cs \
	Mono.TextEditor.Theatrics/SmartScrolledWindow.cs \
	Mono.TextEditor.Theatrics/Stage.cs \
	Mono.TextEditor.Vi/ViActionMaps.cs \
	Mono.TextEditor.Vi/ViActions.cs \
	Mono.TextEditor.Vi/ViMacro.cs \
	Mono.TextEditor.Vi/ViMark.cs \
	Mono.TextEditor.Vi/ViMode.cs \
	Mono.TextEditor.Vi/ViWordFindStrategy.cs \
	Mono.TextEditor/BookmarkActions.cs \
	Mono.TextEditor/BookmarkMarker.cs \
	Mono.TextEditor/BufferedTextReader.cs \
	Mono.TextEditor/Caret.cs \
	Mono.TextEditor/CaretMoveActions.cs \
	Mono.TextEditor/ClipboardActions.cs \
	Mono.TextEditor/CodeSegmentEditorWindow.cs \
	Mono.TextEditor/CodeSegmentPreviewWindow.cs \
	Mono.TextEditor/DashedLineMargin.cs \
	Mono.TextEditor/DefaultEditActions.cs \
	Mono.TextEditor/DeleteActions.cs \
	Mono.TextEditor/Document.cs \
	Mono.TextEditor/DocumentLocation.cs \
	Mono.TextEditor/DocumentUpdateRequest.cs \
	Mono.TextEditor/EditMode.cs \
	Mono.TextEditor/EmacsWordFindStrategy.cs \
	Mono.TextEditor/FoldActions.cs \
	Mono.TextEditor/FoldingScreenbackgroundRenderer.cs \
	Mono.TextEditor/FoldingType.cs \
	Mono.TextEditor/FoldMarkerMargin.cs \
	Mono.TextEditor/FoldSegment.cs \
	Mono.TextEditor/GapBuffer.cs \
	Mono.TextEditor/GutterMargin.cs \
	Mono.TextEditor/HelperMethods.cs \
	Mono.TextEditor/HslColor.cs \
	Mono.TextEditor/IAnimationDrawer.cs \
	Mono.TextEditor/IBackgroundRenderer.cs \
	Mono.TextEditor/IBracketMatcher.cs \
	Mono.TextEditor/IBuffer.cs \
	Mono.TextEditor/IconMargin.cs \
	Mono.TextEditor/InsertionCursorEditMode.cs \
	Mono.TextEditor/ISearchEngine.cs \
	Mono.TextEditor/ISegment.cs \
	Mono.TextEditor/ITextEditorOptions.cs \
	Mono.TextEditor/ITooltipProvider.cs \
	Mono.TextEditor/IWordFindStrategy.cs \
	Mono.TextEditor/LineEventArgs.cs \
	Mono.TextEditor/LineSegment.cs \
	Mono.TextEditor/LineSegmentTree.cs \
	Mono.TextEditor/LineSplitter.cs \
	Mono.TextEditor/LinkEventArgs.cs \
	Mono.TextEditor/Margin.cs \
	Mono.TextEditor/MarginDrawer.cs \
	Mono.TextEditor/PangoUtil.cs \
	Mono.TextEditor/PieceTable.cs \
	Mono.TextEditor/Platform.cs \
	Mono.TextEditor/RedBlackTree.cs \
	Mono.TextEditor/ReplaceEventArgs.cs \
	Mono.TextEditor/SearchRequest.cs \
	Mono.TextEditor/SearchResult.cs \
	Mono.TextEditor/Segment.cs \
	Mono.TextEditor/Selection.cs \
	Mono.TextEditor/SelectionActions.cs \
	Mono.TextEditor/SharpDevelopWordFindStrategy.cs \
	Mono.TextEditor/SimpleEditMode.cs \
	Mono.TextEditor/TextEditor.cs \
	Mono.TextEditor/TextEditorAccessibility.cs \
	Mono.TextEditor/TextEditorContainer.cs \
	Mono.TextEditor/TextEditorData.cs \
	Mono.TextEditor/TextEditorOptions.cs \
	Mono.TextEditor/TextLinkEditMode.cs \
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
	gtk-gui/gui.stetic \
	Styles/BrownStyle.xml \
	Styles/C64Style.xml \
	Styles/IrBlackStyle.xml \
	Styles/OblivionStyle.xml \
	Styles/Styles.xsd \
	Styles/TangoLightStyle.xml \
	Styles/VisualStudioStyle.xml \
	SyntaxModes/ActionScriptSyntaxMode.xml \
	SyntaxModes/AspNetSyntaxMode.xml \
	SyntaxModes/BooSyntaxMode.xml \
	SyntaxModes/ChangeLogSyntaxMode.xml \
	SyntaxModes/CPPSyntaxMode.xml \
	SyntaxModes/CSharpSyntaxMode.xml \
	SyntaxModes/CssSyntaxMode.xml \
	SyntaxModes/CSyntaxMode.xml \
	SyntaxModes/DiffSyntaxMode.xml \
	SyntaxModes/ILSyntaxMode.xml \
	SyntaxModes/JavascriptSyntaxMode.xml \
	SyntaxModes/JavaSyntaxMode.xml \
	SyntaxModes/JaySyntaxMode.xml \
	SyntaxModes/LuaSyntaxMode.xml \
	SyntaxModes/MakefileSyntaxMode.xml \
	SyntaxModes/PythonSyntaxMode.xml \
	SyntaxModes/RubySyntaxMode.xml \
	SyntaxModes/ValaSyntaxMode.xml \
	SyntaxModes/VBNetSyntaxMode.xml \
	SyntaxModes/XmlSyntaxMode.xml

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -unsafe $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

$(ASSEMBLY).config: $(srcdir)/$(CONFIG)
	cp "$(srcdir)/$(CONFIG)" $(ASSEMBLY).config

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES) $(CONFIG)

include $(top_srcdir)/Makefile.include
