// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;

using MonoDevelop.Gui;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor;
using MonoDevelop.EditorBindings.Search;

namespace MonoDevelop.TextEditor.Document
{
	public class SearchReplaceManager
	{
		public static ReplaceDialog ReplaceDialog     = null;
				
		static IFind find                  = new DefaultFind();
		static SearchOptions searchOptions = new SearchOptions("SharpDevelop.SearchAndReplace.SearchAndReplaceProperties");

		
		public static SearchOptions SearchOptions {
			get {
				return searchOptions;
			}
		}
		
		static SearchReplaceManager()
		{
			find.TextIteratorBuilder = new ForwardTextIteratorBuilder();
			searchOptions.SearchStrategyTypeChanged   += new EventHandler(InitializeSearchStrategy);
			searchOptions.DocumentIteratorTypeChanged += new EventHandler(InitializeDocumentIterator);
			InitializeDocumentIterator(null, null);
			InitializeSearchStrategy(null, null);
		}	
		
		static void InitializeSearchStrategy(object sender, EventArgs e)
		{
			find.SearchStrategy = SearchReplaceUtilities.CreateSearchStrategy(SearchOptions.SearchStrategyType);
		}
		
		static void InitializeDocumentIterator(object sender, EventArgs e)
		{
			find.DocumentIterator = SearchReplaceUtilities.CreateDocumentIterator(SearchOptions.DocumentIteratorType);
		}
		
		// TODO: Transform Replace Pattern
		public static void Replace()
		{
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null) {
				TextEditorControl textarea = ((ITextEditorControlProvider)WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent).TextEditorControl;
				string text = textarea.ActiveTextAreaControl.TextArea.SelectionManager.SelectedText;
				if (text == SearchOptions.SearchPattern) {
					int offset = textarea.ActiveTextAreaControl.TextArea.SelectionManager.SelectionCollection[0].Offset;
					
					textarea.BeginUpdate();
					textarea.ActiveTextAreaControl.TextArea.SelectionManager.RemoveSelectedText();
					textarea.Document.Insert(offset, SearchOptions.ReplacePattern);
					textarea.ActiveTextAreaControl.Caret.Position = textarea.Document.OffsetToPosition(offset +  SearchOptions.ReplacePattern.Length);
					textarea.EndUpdate();
				}
			}
			FindNext();
		}
		
		public static void MarkAll()
		{
			TextEditorControl textArea = null;
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null) {
				textArea = ((ITextEditorControlProvider)WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent).TextEditorControl;
				textArea.ActiveTextAreaControl.TextArea.SelectionManager.ClearSelection();
			}
			find.Reset();
			find.SearchStrategy.CompilePattern(searchOptions);
			while (true) {
				ISearchResult result = SearchReplaceManager.find.FindNext(searchOptions);
				
				if (result == null) {
					//MessageBox.Show((Form)WorkbenchSingleton.Workbench, "Mark all done", "Finished");
					find.Reset();
					return;
				} else {
					textArea = OpenTextArea(result.FileName); 
					
					textArea.ActiveTextAreaControl.Caret.Position = textArea.Document.OffsetToPosition(result.Offset);
					int lineNr = textArea.Document.GetLineNumberForOffset(result.Offset);
					
					if (!textArea.Document.BookmarkManager.IsMarked(lineNr)) {
						textArea.Document.BookmarkManager.ToggleMarkAt(lineNr);
					}
				}
			}
		}
		
		public static void ReplaceAll()
		{
			TextEditorControl textArea = null;
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null) {
				textArea = ((ITextEditorControlProvider)WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent).TextEditorControl;
				textArea.ActiveTextAreaControl.TextArea.SelectionManager.ClearSelection();
			}
			find.Reset();
			find.SearchStrategy.CompilePattern(searchOptions);
			
			while (true) {
				ISearchResult result = SearchReplaceManager.find.FindNext(SearchReplaceManager.searchOptions);
				
				if (result == null) {
					//MessageBox.Show((Form)WorkbenchSingleton.Workbench, "Replace all done", "Finished");
					find.Reset();
					return;
				} else {
					textArea = OpenTextArea(result.FileName); 
					
					textArea.BeginUpdate();
					textArea.ActiveTextAreaControl.TextArea.SelectionManager.SelectionCollection.Clear();
					
					string transformedPattern = result.TransformReplacePattern(SearchOptions.ReplacePattern);
					find.Replace(result.Offset,
					             result.Length, 
					             transformedPattern);
					textArea.EndUpdate();
					textArea.Refresh();
				}
			}
		}
		
		static ISearchResult lastResult = null;
		public static void FindNext()
		{
			if (find == null || 
			    searchOptions.SearchPattern == null || 
			    searchOptions.SearchPattern.Length == 0) {
				return;
			}
			
			find.SearchStrategy.CompilePattern(searchOptions);
			ISearchResult result = find.FindNext(searchOptions);
				
			if (result == null) {
				ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
				//MessageBox.Show((Form)WorkbenchSingleton.Workbench,
				//                resourceService.GetString("Dialog.NewProject.SearchReplace.SearchStringNotFound"),
				//                "Not Found", 
				//                MessageBoxButtons.OK, 
				//                MessageBoxIcon.Information);
				find.Reset();
			} else {
				TextEditorControl textArea = OpenTextArea(result.FileName);
				
				if (lastResult != null  && lastResult.FileName == result.FileName && 
				    textArea.ActiveTextAreaControl.Caret.Offset != lastResult.Offset + lastResult.Length) {
					find.Reset();
				}
				int startPos = Math.Min(textArea.Document.TextLength, Math.Max(0, result.Offset));
				int endPos   = Math.Min(textArea.Document.TextLength, startPos + result.Length);
				
				textArea.ActiveTextAreaControl.Caret.Position = textArea.Document.OffsetToPosition(endPos);
				textArea.ActiveTextAreaControl.TextArea.SelectionManager.ClearSelection();
				textArea.ActiveTextAreaControl.TextArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, textArea.Document.OffsetToPosition(startPos),
				                                                                                           textArea.Document.OffsetToPosition(endPos)));
				textArea.Refresh();
			}
			
			lastResult = result;
		}
		
		static TextEditorControl OpenTextArea(string fileName) 
		{
			if (fileName != null) {
				IFileService fileService = (IFileService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IFileService));
				fileService.OpenFile(fileName);
			}
			
			return ((ITextEditorControlProvider)WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent).TextEditorControl;
		}
	}	
}
