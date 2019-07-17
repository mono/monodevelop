//
// FindInFilesController.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.FindInFiles
{
	class FindInFilesController
	{
		FindInFilesDialog currentFindDialog;

		void Dispose ()
		{
			currentFindDialog?.Destroy ();
			currentFindDialog = null;
		}

		FindInFilesModel model;
		Scope currentScope;
		Task<ImmutableArray<FileProvider>> fileProviderTask;

		public FindInFilesController (FindInFilesModel model)
		{
			this.model = model;
			currentFindDialog = new FindInFilesDialog (model);
			currentFindDialog.Destroyed += (sender, e) => {
				StoreFindModel (model);
				currentFindDialog = null;
			};
			MessageService.PlaceDialog (currentFindDialog, null);
			currentFindDialog.Present ();

			model.CurrentScopeChanged += StartScopeTask;
			model.FileMaskChanged += StartScopeTask;
			model.RecurseSubdirectoriesChanged += StartScopeTask;
			model.FindInFilesPathChanged += StartScopeTask;

			currentFindDialog.RequestFindAndReplace += delegate {
				SearchReplace (model, currentScope, fileProviderTask, currentFindDialog.UpdateStopButton, currentFindDialog.UpdateResultPad);
			};

			StartScopeTask (this, EventArgs.Empty);
		}

		void StartScopeTask (object sender, EventArgs e)
		{
			var scope = Scope.Create (model);
			if (scope == null)
				return;
			currentScope = scope;
			fileProviderTask = scope.GetFilesAsync (model);
		}

		internal static void SearchReplace (FindInFilesModel model, Scope scope, Task<ImmutableArray<FileProvider>> getFilesTask, System.Action UpdateStopButton, System.Action<SearchResultPad> UpdateResultPad)
		{
			if (find != null && find.IsRunning) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("There is a search already in progress. Do you want to stop it?"), AlertButton.Stop))
					return;
			}
			if (!scope.ValidateSearchOptions (model))
				return;
			searchTokenSource.Cancel ();

			if (getFilesTask == null)
				return;

			find = new FindInFilesSession ();

			if (string.IsNullOrEmpty (model.FindPattern))
				return;
			if (!find.ValidatePattern (model, model.FindPattern)) {
				MessageService.ShowError (GettextCatalog.GetString ("Search pattern is invalid"));
				return;
			}

			if (model.ReplacePattern != null && !find.ValidatePattern (model, model.ReplacePattern)) {
				MessageService.ShowError (GettextCatalog.GetString ("Replace pattern is invalid"));
				return;
			}
			var cancelSource = new CancellationTokenSource ();
			searchTokenSource = cancelSource;
			var token = cancelSource.Token;

			currentTask = Task.Run (async delegate {
				var files = await getFilesTask;
				using (var searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true)) {

					if (UpdateResultPad != null) {
						Application.Invoke ((o, args) => {
							UpdateResultPad (searchMonitor.ResultPad);
						});
					}

					if (UpdateStopButton != null) {
						Application.Invoke ((o, args) => {
							UpdateStopButton ();
						});
					}
					var timer = DateTime.Now;
					string errorMessage = null;

					try {
						var results = new List<SearchResult> ();
						searchMonitor.BeginTask (scope.GetDescription (model), 150);
						try {
							foreach (var result in find.FindAll (model, files, searchMonitor, token)) {
								if (token.IsCancellationRequested)
									return;
								results.Add (result);
							}
							searchMonitor.ReportResults (results);
						} finally {
							searchMonitor.EndTask ();
						}
					} catch (Exception ex) {
						errorMessage = ex.Message;
						LoggingService.LogError ("Error while search", ex);
					}
					string message = null;
					if (errorMessage != null) {
						message = GettextCatalog.GetString ("The search could not be finished: {0}", errorMessage);
						searchMonitor.ReportError (message, null);
					} else if (!searchMonitor.CancellationToken.IsCancellationRequested) {
						string matches = string.Format (GettextCatalog.GetPluralString ("{0} match found", "{0} matches found", find.FoundMatchesCount), find.FoundMatchesCount);
						string fileString = string.Format (GettextCatalog.GetPluralString ("in {0} file.", "in {0} files.", find.SearchedFilesCount), find.SearchedFilesCount);
						message = GettextCatalog.GetString ("Search completed.") + Environment.NewLine + matches + " " + fileString;
						searchMonitor.ReportSuccess (message);
					}
					if (message != null)
						searchMonitor.ReportStatus (message);
					searchMonitor.Log.WriteLine (GettextCatalog.GetString ("Search time: {0} seconds."), (DateTime.Now - timer).TotalSeconds);
				}
				if (UpdateStopButton != null) {
					Application.Invoke ((o, args) => {
						UpdateStopButton ();
					});
				}
			});
		}

		#region Static methods

		static FindInFilesController currentFindInFilesController;
		static FindInFilesSession find;
		static CancellationTokenSource searchTokenSource = new CancellationTokenSource ();
		static Task currentTask;

		public static bool IsSearchRunning { get => currentTask != null && !currentTask.IsCompleted; }

		public static void ShowFind ()
		{
			var model = CreateModelFromIdeSettings ();
			model.InReplaceMode = false;
			ShowSingleInstance (model);
		}

		public static void ShowReplace ()
		{
			var model = CreateModelFromIdeSettings ();
			model.InReplaceMode = true;
			ShowSingleInstance (model);
		}

		public static void FindInPath (string path)
		{
			var model = CreateModelFromIdeSettings ();
			model.SearchScope = SearchScope.Directories;
			model.FindInFilesPath = path;
			ShowSingleInstance (model);
		}

		static void ShowSingleInstance (FindInFilesModel model)
		{
			if (currentFindInFilesController != null)
				currentFindInFilesController.Dispose ();
			currentFindInFilesController = new FindInFilesController (model);
		}

		static FindInFilesModel CreateModelFromIdeSettings ()
		{
			var result = new FindInFilesModel ();
			var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());

			result.RecurseSubdirectories = properties.Get ("SearchPathRecursively", true);
			result.RegexSearch = properties.Get ("RegexSearch", false);
			result.FileMask = properties.Get ("MonoDevelop.FindReplaceDialogs.FileMask", "");
			result.SearchScope = (SearchScope)properties.Get ("Scope", (int)SearchScope.WholeWorkspace);
			result.CaseSensitive = properties.Get ("CaseSensitive", false);
			result.WholeWordsOnly = properties.Get ("WholeWordsOnly", false);
			result.RegexSearch = properties.Get ("RegexSearch", false);

			return result;
		}

		static void StoreFindModel (FindInFilesModel model)
		{
			if (model == null)
				throw new System.ArgumentNullException (nameof (model));

			var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());

			properties.Set ("SearchPathRecursively", model.RecurseSubdirectories);
			properties.Set ("RegexSearch", model.RegexSearch);
			properties.Set ("MonoDevelop.FindReplaceDialogs.FileMask", model.FileMask);
			properties.Set ("Scope", (int)model.SearchScope);
			properties.Set ("CaseSensitive", model.CaseSensitive);
			properties.Set ("WholeWordsOnly", model.WholeWordsOnly);
			properties.Set ("RegexSearch", model.RegexSearch);
		}

		internal static void Stop ()
		{
			searchTokenSource.Cancel ();
		}
		#endregion

	}
}
