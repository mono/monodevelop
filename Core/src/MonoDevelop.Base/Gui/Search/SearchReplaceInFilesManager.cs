// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.Pads;

using Gtk;

namespace MonoDevelop.Gui.Search
{
	public class SearchReplaceInFilesManager
	{
		internal static ReplaceInFilesDialog ReplaceDialog;

		static IFind find                  = new DefaultFind();
		static SearchOptions searchOptions = new SearchOptions("SharpDevelop.SearchAndReplace.SearchAndReplaceInFilesProperties");
		
		static DateTime timer;
		static bool searching;
		static bool cancelled;
		static string searchError;
		static ISearchProgressMonitor searchMonitor;
		
		public static SearchOptions SearchOptions {
			get {
				return searchOptions;
			}
		}
		
		static SearchReplaceInFilesManager()
		{
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
		/// This method displays the search result in the search results pad
		/// </remarks>
		static void DisplaySearchResult(ISearchResult result)
		{
			if (result.Line != -1) {
				string text = result.DocumentInformation.GetLineTextAtOffset (result.DocumentOffset);
				searchMonitor.ReportResult (result.FileName, result.Line, result.Column, text);
			} else {
				string msg = string.Format (GettextCatalog.GetString ("Match at offset {0}"), result.DocumentOffset);
				searchMonitor.ReportResult (result.FileName, 0, 0, msg);
			}
		}
		
		static bool InitializeSearchInFiles()
		{
			Debug.Assert(searchOptions != null);
			cancelled = false;
			
			searchMonitor = Runtime.TaskService.GetSearchProgressMonitor (true);
			searchMonitor.CancelRequested += (MonitorHandler) Runtime.DispatchService.GuiDispatch (new MonitorHandler (OnCancelRequested));
			
			InitializeDocumentIterator(null, null);
			InitializeSearchStrategy(null, null);
			find.Reset();
			
			try {
				find.SearchStrategy.CompilePattern(searchOptions);
			} catch {
				Runtime.MessageService.ShowMessage (GettextCatalog.GetString ("Search pattern is invalid"), DialogPointer);
				return false;
			}
			return true;
		}
		
		static void OnCancelRequested (IProgressMonitor monitor)
		{
			CancelSearch ();
		}
		
		static void FinishSearchInFiles ()
		{
			string msg;
			if (searchError != null)
				msg = string.Format (GettextCatalog.GetString ("The search could not be finished: {0}"), searchError);
			else if (cancelled)
				msg = GettextCatalog.GetString ("Search cancelled.");
			else
				msg = string.Format (GettextCatalog.GetString ("Search completed. {0} matches found in {1} files."), find.MatchCount, find.SearchedFileCount);
				
			searchMonitor.ReportResult (null, 0, 0, msg);
			
			searchMonitor.Log.WriteLine (msg);
			searchMonitor.Log.WriteLine (GettextCatalog.GetString ("Search time: {0} seconds."), (DateTime.Now - timer).TotalSeconds);

			searchMonitor.Dispose ();
			searching = false;
		}
		
		public static void ReplaceAll()
		{
			if (searching) {
				if (!Runtime.MessageService.AskQuestion (GettextCatalog.GetString ("There is a search already in progress. Do you want to cancel it?")))
					return;
				CancelSearch ();
			}
			
			if (!InitializeSearchInFiles()) {
				return;
			}
			
			string msg = string.Format (GettextCatalog.GetString ("Replacing '{0}' in {1}."), searchOptions.SearchPattern, searchOptions.SearchDirectory);
			searchMonitor.ReportResult (null, 0, 0, msg);
			
			timer = DateTime.Now;
			Runtime.DispatchService.BackgroundDispatch (new MessageHandler(ReplaceAllThread));
		}
		
		static void ReplaceAllThread()
		{
			searching = true;
			searchError = null;
			
			while (!cancelled) 
			{
				try
				{
					ISearchResult result = find.FindNext(searchOptions);
					if (result == null) {
						break;
					}
					
					find.Replace(result, result.TransformReplacePattern(SearchOptions.ReplacePattern));
					DisplaySearchResult (result);
				}
				catch (Exception ex) 
				{
					searchError = ex.Message;
					break;
				}
			}
			
			FinishSearchInFiles ();
		}
		
		public static void FindAll()
		{
			if (searching) {
				if (!Runtime.MessageService.AskQuestion (GettextCatalog.GetString ("There is a search already in progress. Do you want to cancel it?")))
					return;
				CancelSearch ();
			}
			
			if (!InitializeSearchInFiles()) {
				return;
			}
			
			string msg = string.Format (GettextCatalog.GetString ("Looking for '{0}' in {1}."), searchOptions.SearchPattern, searchOptions.SearchDirectory);
			searchMonitor.ReportResult (null, 0, 0, msg);
			
			timer = DateTime.Now;
			Runtime.DispatchService.BackgroundDispatch (new MessageHandler(FindAllThread));
		}
		
		static void FindAllThread()
		{
			searching = true;
			searchError = null;
			
			while (!cancelled) 
			{
				try
				{
					ISearchResult result = find.FindNext (searchOptions);
					if (result == null) {
						break;
					}

					DisplaySearchResult (result);
				}
				catch (Exception ex)
				{
					searchMonitor.Log.WriteLine (ex);
					searchError = ex.Message;
					break;
				}
			}
			
			FinishSearchInFiles ();
		}
		
		public static void CancelSearch ()
		{
			if (!searching) return;
			cancelled = true;
			find.Cancel ();
		}

		internal static Gtk.Dialog DialogPointer
		{
			get {
				if ( ReplaceDialog != null ){ 
					return ReplaceDialog.DialogPointer;
				}
				return null;
			}
		}
		
	}
}
