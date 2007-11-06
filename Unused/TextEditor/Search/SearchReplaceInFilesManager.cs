//  SearchReplaceInFilesManager.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.Diagnostics;

using MonoDevelop.Core.Gui;

using MonoDevelop.Core;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.EditorBindings.Search;

namespace MonoDevelop.TextEditor.Document
{
	public class SearchReplaceInFilesManager
	{
		static IFind find                  = new DefaultFind();
		static SearchOptions searchOptions = new SearchOptions("SharpDevelop.SearchAndReplace.SearchAndReplaceInFilesProperties");
		
		static PropertyService      propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		
		static string              currentFileName = String.Empty;
		static IDocument currentDocument = null;
		
		public static SearchOptions SearchOptions {
			get {
				return searchOptions;
			}
		}
		
		static SearchReplaceInFilesManager()
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
		
		/// <remarks>
		/// This method displays the search result in the task view
		/// </remarks>
		static void DisplaySearchResult(ISearchResult result)
		{
			TaskService taskService = (TaskService)MonoDevelop.Core.ServiceManager.Services.GetService(typeof(TaskService));
			
			// check if the current document is up to date
			if (currentFileName != result.FileName) {
				// if not, create new document
				currentFileName = result.FileName;
				currentDocument = result.CreateDocument(); 
			}
			
			// get line out of the document and display it in the task list
			int lineNumber = currentDocument.GetLineNumberForOffset(Math.Min(currentDocument.TextLength, result.Offset));
			LineSegment line = currentDocument.GetLineSegment(lineNumber);
			taskService.Tasks.Add(new Task(result.FileName, currentDocument.GetText(line.Offset, line.Length), result.Offset - line.Offset, lineNumber));
		}
		
		static bool InitializeSearchInFiles()
		{
			Debug.Assert(searchOptions != null);
			
			TaskService taskService = (TaskService)MonoDevelop.Core.ServiceManager.Services.GetService(typeof(TaskService));
			taskService.Tasks.Clear();
			
			InitializeDocumentIterator(null, null);
			InitializeSearchStrategy(null, null);
			find.Reset();
			find.SearchStrategy.CompilePattern(searchOptions);
			
			currentFileName = String.Empty;
			currentDocument = null;
			
			return true;
		}
		
		static void FinishSearchInFiles()
		{
			TaskService taskService = (TaskService)MonoDevelop.Core.ServiceManager.Services.GetService(typeof(TaskService));
			taskService.NotifyTaskChange();
			
			OpenTaskView taskView = WorkbenchSingleton.Workbench.GetPad(typeof(OpenTaskView)) as OpenTaskView;
			if (taskView != null) taskView.BringToFront();
		}
		
		public static void ReplaceAll()
		{
			if (!InitializeSearchInFiles()) {
				return;
			}
			
			while (true) {
				ISearchResult result = find.FindNext(searchOptions);
				if (result == null) {
					break;
				}
				
				find.Replace(result.Offset, 
				             result.Length, 
				             result.TransformReplacePattern(SearchOptions.ReplacePattern));
				
				DisplaySearchResult(result);
			}
			
			FinishSearchInFiles();
		}
		
		public static void FindAll()
		{
			if (!InitializeSearchInFiles()) {
				return;
			}
			
			while (true) {
				ISearchResult result = find.FindNext(searchOptions);
				if (result == null) {
					break;
				}
				
				DisplaySearchResult(result);
			}
			
			FinishSearchInFiles();
		}
	}
}
