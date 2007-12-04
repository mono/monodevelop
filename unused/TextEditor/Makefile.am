SDDIR=../../../SharpDevelop/src/Main/Core
FILES = \
./Gui/Editor/ParserFoldingStrategy.cs \
./Gui/Editor/SharpDevelopTextEditorProperties.cs \
./Gui/Editor/SharpDevelopTextAreaControl.cs \
./Gui/Editor/InsightWindow/IndexerInsightDataProvider.cs \
./Gui/Editor/InsightWindow/MethodInsightDataProvider.cs \
./Gui/Editor/CompletionWindow/TemplateCompletionDataProvider.cs \
./Gui/Editor/CompletionWindow/CodeCompletionDataProvider.cs \
./Gui/Editor/CompletionWindow/CodeCompletionData.cs \
./Gui/Editor/CompletionWindow/CommentCompletionDataProvider.cs \
./Gui/Editor/ErrorDrawer.cs \
./Gui/Editor/ITextAreaControlProvider.cs \
./Gui/Editor/TextEditorDisplayBinding.cs \
./Gui/Dialogs/ExportProjectToHtmlDialog.cs \
./Gui/Dialogs/SortOptionsDialog.cs \
./Gui/Dialogs/ReplaceInFilesDialog.cs \
./Gui/Dialogs/ReplaceDialog.cs \
./Conditions/TextContentCondition.cs \
./Actions.cs \
./Search/DocumentIterator/ProvidedDocumentInformation.cs \
./Search/DocumentIterator/DirectoryDocumentIterator.cs \
./Search/DocumentIterator/AllOpenDocumentIterator.cs \
./Search/DocumentIterator/IDocumentIterator.cs \
./Search/DocumentIterator/ReverseDocumentIterator.cs \
./Search/DocumentIterator/WholeProjectDocumentIterator.cs \
./Search/DocumentIterator/CurrentDocumentIterator.cs \
./Search/SearchReplaceManager.cs \
./Search/DefaultFind.cs \
./Search/SearchResult/DefaultSearchResult.cs \
./Search/SearchResult/ISearchResult.cs \
./Search/SearchReplaceInFilesManager.cs \
./Search/SearchReplaceUtilities.cs \
./Search/IFind.cs \
./Search/TextIterator/ITextIteratorBuilder.cs \
./Search/TextIterator/ForwardTextIteratorBuilder.cs \
./Search/TextIterator/ForwardTextIterator.cs \
./AssemblyInfo.cs \
./XmlFormattingStrategy.cs

DLL=../../../../build/bin/MonoDevelop.DefaultTexteditor.dll

all: $(DLL)

$(DLL): $(FILES) ../../../../data/resources/glade/texteditoraddin.glade
	$(CSC) $(CSC_FLAGS) /out:$(DLL) /target:library /r:System.Drawing \
		/r:../../../../build/bin/MonoDevelop.Core.dll \
		/r:../../../../build/bin/MonoDevelop.Base.dll \
		/r:../../../../build/bin/MonoDevelop.SourceEditor.dll \
		/r:System.DirectoryServices /r:pango-sharp /r:glade-sharp \
		/r:../../../../build/bin/MonoDevelop.Gui.Widgets.dll \
		/r:glib-sharp \
		/resource:../../../../data/resources/glade/texteditoraddin.glade,texteditoraddin.glade \
		/define:GTK /r:gtk-sharp /r:gdk-sharp /r:gnome-sharp\
		$(FILES)

CLEANFILES=$(DLL) $(DLL).mdb

EXTRA_DIST = $(FILES)

