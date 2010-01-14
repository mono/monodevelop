// 
// FindInFilesJob.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Ide.Jobs;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Ide.FindInFiles
{
	public class FindInFilesJob: SearchJob
	{
		static List<IProgressMonitor> searchesInProgress = new List<IProgressMonitor> ();
		string pattern;
		FilterOptions options;
		Scope scope;
		string replacePattern;

		public FindInFilesJob (string pattern, string replacePattern, Scope scope, FilterOptions options)
		{
			this.pattern = pattern;
			this.options = options;
			this.scope = scope;
			this.replacePattern = replacePattern;
			Title = GettextCatalog.GetString ("Find in Files");
			Description = scope.GetDescription (options, pattern, replacePattern);
			Icon = MonoDevelop.Core.Gui.Stock.FindInFiles;
		}
		
		public static bool IsSearchRunning {
			get {
				lock (searchesInProgress) {
					return searchesInProgress.Count > 0;
				}
			}
		}
		
		public static void CancelAll ()
		{
			lock (searchesInProgress) {
				foreach (IProgressMonitor monitor in searchesInProgress)
					monitor.AsyncOperation.Cancel ();
			}
		}
		
		public override bool Reusable {
			get {
				return true;
			}
		}

		
		protected override void OnRun (ISearchProgressMonitor searchMonitor)
		{
			lock (searchesInProgress)
				searchesInProgress.Add (searchMonitor);

			FindReplace find = new FindReplace ();
			searchMonitor.ReportStatus (scope.GetDescription (options, pattern, null));

			if (!find.ValidatePattern (options, pattern)) {
				searchMonitor.Dispose ();
				MessageService.ShowError (GettextCatalog.GetString ("Search pattern is invalid"));
				return;
			}

			if (replacePattern != null && !find.ValidatePattern (options, replacePattern)) {
				searchMonitor.Dispose ();
				MessageService.ShowError (GettextCatalog.GetString ("Replace pattern is invalid"));
				return;
			}

			DispatchService.BackgroundDispatch (delegate {
				DateTime timer = DateTime.Now;
				string errorMessage = null;
				
				try {
					List<SearchResult> results = new List<SearchResult> ();
					foreach (SearchResult result in find.FindAll (scope, searchMonitor, pattern, replacePattern, options)) {
						if (searchMonitor.IsCancelRequested)
							break;
						results.Add (result);
						if (results.Count > 10) {
							Application.Invoke (delegate {
								results.ForEach (r => searchMonitor.ReportResult (r));
								results.Clear ();
							});
						}
					}
					Application.Invoke (delegate {
						results.ForEach (r => searchMonitor.ReportResult (r));
						results.Clear ();
					});
				} catch (Exception ex) {
					errorMessage = ex.Message;
					LoggingService.LogError ("Error while search", ex);
				}
				
				string message;
				if (errorMessage != null) {
					message = GettextCatalog.GetString ("The search could not be finished: {0}", errorMessage);
				} else if (searchMonitor.IsCancelRequested) {
					message = GettextCatalog.GetString ("Search cancelled.");
				} else {
					string matches = string.Format (GettextCatalog.GetPluralString ("{0} match found", "{0} matches found", find.FoundMatchesCount), find.FoundMatchesCount);
					string files = string.Format (GettextCatalog.GetPluralString ("in {0} file.", "in {0} files.", find.SearchedFilesCount), find.SearchedFilesCount);
					message = GettextCatalog.GetString ("Search completed. ") + matches + " " + files;
				}
				searchMonitor.ReportStatus (message);
				searchMonitor.Log.WriteLine (GettextCatalog.GetString ("Search time: {0} seconds."), (DateTime.Now - timer).TotalSeconds);
				searchMonitor.ReportSuccess (message);
				searchMonitor.Dispose ();
				lock (searchesInProgress)
					searchesInProgress.Remove (searchMonitor);
			});
		}

		public override void FillExtendedStatusPanel (JobInstance jobi, Gtk.HBox expandedPanel, out Gtk.Widget mainWidget)
		{
			base.FillExtendedStatusPanel (jobi, expandedPanel, out mainWidget);
			Gtk.Button b = new Gtk.Button (GettextCatalog.GetString ("Search Results"));
			b.Relief = Gtk.ReliefStyle.None;
			b.Show ();
			expandedPanel.PackStart (b, false, false, 0);
			expandedPanel.ReorderChild (b, 0);
			mainWidget = b;
		}
	}
}
