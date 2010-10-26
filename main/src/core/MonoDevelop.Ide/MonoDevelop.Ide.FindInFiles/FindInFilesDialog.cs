// 
// FindInFilesDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System.Linq;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.FindInFiles
{
	public partial class FindInFilesDialog : Gtk.Dialog
	{
		readonly bool writeScope = true;
		readonly bool showReplace;
		
		const int ScopeWholeSolution   = 0;
		const int ScopeCurrentProject  = 1;
		const int ScopeAllOpenFiles    = 2;
		const int ScopeDirectories     = 3;
		const int ScopeCurrentDocument = 4;
		const int ScopeSelection       = 5;
		
		FindInFilesDialog (bool showReplace, string directory) : this(showReplace)
		{
			comboboxScope.Active = ScopeDirectories;
			comboboxentryPath.Entry.Text = directory;
			writeScope = false;
		}

		readonly ComboBoxEntry comboboxentryReplace;
		readonly Label labelReplace;

		FindInFilesDialog (bool showReplace)
		{
			this.showReplace = showReplace;
			Build ();
			Title = showReplace ? GettextCatalog.GetString ("Replace in Files") : GettextCatalog.GetString ("Find in Files");
			TransientFor = IdeApp.Workbench.RootWindow;
			if (!showReplace)
				buttonReplace.Destroy();

			if (showReplace) {
				tableFindAndReplace.NRows = 4;
				labelReplace = new Label {Text = GettextCatalog.GetString("_Replace:"), Xalign = 0f, UseUnderline = true};
				tableFindAndReplace.Add (labelReplace);

				comboboxentryReplace = new ComboBoxEntry ();
				tableFindAndReplace.Add (comboboxentryReplace);

				var childLabel = (Table.TableChild)tableFindAndReplace[labelReplace];
				childLabel.TopAttach = 1;
				childLabel.BottomAttach = 2;
				childLabel.XOptions = childLabel.YOptions = (AttachOptions)4;

				var childCombo = (Table.TableChild)tableFindAndReplace[comboboxentryReplace];
				childCombo.TopAttach = 1;
				childCombo.BottomAttach = 2;
				childCombo.LeftAttach = 1;
				childCombo.RightAttach = 2;
				childCombo.XOptions = childCombo.YOptions = (AttachOptions)4;

				childLabel = (Table.TableChild)tableFindAndReplace[labelScope];
				childLabel.TopAttach = 2;
				childLabel.BottomAttach = 3;

				childCombo = (Table.TableChild)tableFindAndReplace[hbox2];
				childCombo.TopAttach = 2;
				childCombo.BottomAttach = 3;

				childCombo = (Table.TableChild)tableFindAndReplace[labelFileMask];
				childCombo.TopAttach = 3;
				childCombo.BottomAttach = 4;

				childCombo = (Table.TableChild)tableFindAndReplace[searchentry1];
				childCombo.TopAttach = 3;
				childCombo.BottomAttach = 4;

				Child.ShowAll ();
			}

			comboboxentryFind.Entry.Activated += delegate { buttonSearch.Click (); };

			buttonReplace.Clicked += HandleReplaceClicked;
			buttonSearch.Clicked += HandleSearchClicked;
			buttonClose.Clicked += ButtonCloseClicked;
			buttonStop.Clicked += ButtonStopClicked;
			var scopeStore = new ListStore (typeof(string));
			scopeStore.AppendValues (GettextCatalog.GetString ("Whole solution"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Current project"));
			scopeStore.AppendValues (GettextCatalog.GetString ("All open files"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Directories"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Current document"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Selection"));
			
			comboboxScope.Model = scopeStore;
		
			comboboxScope.Changed += HandleScopeChanged;

			InitFromProperties ();

			if (IdeApp.Workbench.ActiveDocument != null) {
				var view = IdeApp.Workbench.ActiveDocument.GetContent<ITextBuffer> ();
				if (view != null) {
					string selectedText = view.SelectedText;
					if (!string.IsNullOrEmpty (selectedText)) {
						if (selectedText.Contains ('\n')) {
							comboboxScope.Active = ScopeSelection; 
						} else {
							if (comboboxScope.Active == ScopeSelection)
								comboboxScope.Active = ScopeCurrentDocument;
							comboboxentryFind.Entry.Text = selectedText;
						}
					} else if (comboboxScope.Active == ScopeSelection) {
						comboboxScope.Active = ScopeCurrentDocument;
					}
					
				}
			}
			comboboxentryFind.Entry.SelectRegion (0, comboboxentryFind.ActiveText.Length);
			
			Hidden += delegate { Destroy (); };
			UpdateStopButton ();
			searchentry1.Ready = true;
			searchentry1.Visible = true;
			searchentry1.IsCheckMenu = true;
			
			var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
			
			CheckMenuItem checkMenuItem = searchentry1.AddFilterOption (0, GettextCatalog.GetString ("Include binary files"));
			checkMenuItem.DrawAsRadio = false;
			checkMenuItem.Active = properties.Get ("IncludeBinaryFiles", false);
			checkMenuItem.Toggled += delegate {
				properties.Set ("IncludeBinaryFiles", checkMenuItem.Active);
			};
			
			CheckMenuItem checkMenuItem1 = searchentry1.AddFilterOption (1, GettextCatalog.GetString ("Include hidden files and directories"));
			checkMenuItem1.DrawAsRadio = false;
			checkMenuItem1.Active = properties.Get ("IncludeHiddenFiles", false);
			checkMenuItem1.Toggled += delegate {
				properties.Set ("IncludeHiddenFiles", checkMenuItem1.Active);
			};
			
			Child.Show ();
		}

		void ButtonCloseClicked (object sender, EventArgs e)
		{
			Hide ();
			// Hide destroys the dialog
		}

		Label labelPath;
		ComboBoxEntry comboboxentryPath;
		HBox hboxPath;
		Button buttonBrowsePaths;
		CheckButton checkbuttonRecursively;

		void HandleScopeChanged (object sender, EventArgs e)
		{
			if (hboxPath != null) {
				// comboboxentryPath and buttonBrowsePaths are destroyed with hboxPath
				foreach (Widget w in new Widget[] {
					labelPath,
					hboxPath,
					checkbuttonRecursively
				}) {
					tableFindAndReplace.Remove (w);
					w.Destroy ();
				}
				labelPath = null;
				hboxPath = null;
				comboboxentryPath = null;
				buttonBrowsePaths = null;
				checkbuttonRecursively = null;

				//tableFindAndReplace.NRows = showReplace ? 4u : 3u;

				var childCombo = (Table.TableChild)tableFindAndReplace[labelFileMask];
				childCombo.TopAttach = tableFindAndReplace.NRows - 3;
				childCombo.BottomAttach = tableFindAndReplace.NRows - 2;

				childCombo = (Table.TableChild)tableFindAndReplace[searchentry1];
				childCombo.TopAttach = tableFindAndReplace.NRows - 3;
				childCombo.BottomAttach = tableFindAndReplace.NRows - 2;
			}

			if (comboboxScope.Active == ScopeDirectories) {
				// DirectoryScope
				tableFindAndReplace.NRows = showReplace ? 6u : 5u;
				labelPath = new Label {
					LabelProp = GettextCatalog.GetString("_Path:"),
					UseUnderline = true, 
					Xalign = 0f
				};

				tableFindAndReplace.Add (labelPath);

				var childCombo = (Table.TableChild)tableFindAndReplace[labelPath];
				childCombo.TopAttach = tableFindAndReplace.NRows - 3;
				childCombo.BottomAttach = tableFindAndReplace.NRows - 2;
				childCombo.XOptions = childCombo.YOptions = (AttachOptions)4;

				hboxPath = new HBox ();
				var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
				comboboxentryPath = new ComboBoxEntry ();
				comboboxentryPath.Destroyed += ComboboxentryPathDestroyed;
				LoadHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
				hboxPath.PackStart (comboboxentryPath);

				labelPath.MnemonicWidget = comboboxentryPath;

				var boxChild = (Box.BoxChild)hboxPath[comboboxentryPath];
				boxChild.Position = 0;
				boxChild.Expand = boxChild.Fill = true;

				buttonBrowsePaths = new Button { Label = "..." };
				buttonBrowsePaths.Clicked += ButtonBrowsePathsClicked;
				hboxPath.PackStart (buttonBrowsePaths);
				boxChild = (Box.BoxChild)hboxPath[buttonBrowsePaths];
				boxChild.Position = 1;
				boxChild.Expand = boxChild.Fill = false;

				tableFindAndReplace.Add (hboxPath);
				childCombo = (Table.TableChild)tableFindAndReplace[hboxPath];
				childCombo.TopAttach = tableFindAndReplace.NRows - 3;
				childCombo.BottomAttach = tableFindAndReplace.NRows - 2;
				childCombo.LeftAttach = 1;
				childCombo.RightAttach = 2;
				childCombo.XOptions = childCombo.YOptions = (AttachOptions)4;

				checkbuttonRecursively = new CheckButton {
					Label = GettextCatalog.GetString ("Re_cursively"),
					Active = properties.Get ("SearchPathRecursively", true),
					UseUnderline = true
				};

				checkbuttonRecursively.Destroyed += CheckbuttonRecursivelyDestroyed;
				tableFindAndReplace.Add (checkbuttonRecursively);
				childCombo = (Table.TableChild)tableFindAndReplace[checkbuttonRecursively];
				childCombo.TopAttach = tableFindAndReplace.NRows - 2;
				childCombo.BottomAttach = tableFindAndReplace.NRows - 1;
				childCombo.LeftAttach = 1;
				childCombo.RightAttach = 2;
				childCombo.XOptions = childCombo.YOptions = (AttachOptions)4;

				childCombo = (Table.TableChild)tableFindAndReplace[labelFileMask];
				childCombo.TopAttach = tableFindAndReplace.NRows - 1;
				childCombo.BottomAttach = tableFindAndReplace.NRows;

				childCombo = (Table.TableChild)tableFindAndReplace[searchentry1];
				childCombo.TopAttach = tableFindAndReplace.NRows - 1;
				childCombo.BottomAttach = tableFindAndReplace.NRows;
			}
			Requisition req = SizeRequest ();
			Resize (req.Width, req.Height);
			//	this.QueueResize ();
			ShowAll ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = Math.Max (480, requisition.Width);
		}

		static void ComboboxentryPathDestroyed (object sender, EventArgs e)
		{
			StoreHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", (ComboBoxEntry)sender);
		}

		void ButtonBrowsePathsClicked (object sender, EventArgs e)
		{
			var dlg = new SelectFolderDialog (GettextCatalog.GetString ("Select directory")) {
				TransientFor = this,
			};
			
			string defaultFolder = comboboxentryPath.Entry.Text;
			if (string.IsNullOrEmpty (defaultFolder))
				defaultFolder = IdeApp.ProjectOperations.ProjectsDefaultPath;
			if (!string.IsNullOrEmpty (defaultFolder))
				dlg.CurrentFolder = defaultFolder;
			
			if (dlg.Run ())
				comboboxentryPath.Entry.Text = dlg.SelectedFile;
		}

		static void CheckbuttonRecursivelyDestroyed (object sender, EventArgs e)
		{
			var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
			properties.Set ("SearchPathRecursively", ((CheckButton)sender).Active);
		}

		const char historySeparator = '\n';
		void InitFromProperties ()
		{
			var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
			comboboxScope.Active = properties.Get ("Scope", ScopeWholeSolution);

			//checkbuttonRecursively.Active    = properties.Get ("SearchPathRecursively", true);
			//		checkbuttonFileMask.Active       = properties.Get ("UseFileMask", false);
			checkbuttonCaseSensitive.Active = properties.Get ("CaseSensitive", false);
			checkbuttonWholeWordsOnly.Active = properties.Get ("WholeWordsOnly", false);
			checkbuttonRegexSearch.Active = properties.Get ("RegexSearch", false);

			LoadHistory ("MonoDevelop.FindReplaceDialogs.FindHistory", comboboxentryFind);
			if (showReplace)
				LoadHistory ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", comboboxentryReplace);
			searchentry1.Query = properties.Get ("MonoDevelop.FindReplaceDialogs.FileMask", "");
//			LoadHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
//			LoadHistory ("MonoDevelop.FindReplaceDialogs.FileMaskHistory", comboboxentryFileMask);
		}

		static void LoadHistory (string propertyName, ComboBoxEntry entry)
		{
			entry.Entry.Completion = new EntryCompletion ();
			var store = new ListStore (typeof(string));
			entry.Entry.Completion.Model = store;
			entry.Model = store;
			entry.Entry.ActivatesDefault = true;
			entry.TextColumn = 0;
			var history = PropertyService.Get<string> (propertyName);
			if (!string.IsNullOrEmpty (history)) {
				string[] items = history.Split (historySeparator);
				foreach (string item in items) {
					if (string.IsNullOrEmpty (item))
						continue;
					store.AppendValues (item);
				}
				entry.Entry.Text = items[0];
			}
		}

		void StorePoperties ()
		{
			var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
			if (writeScope)
				properties.Set ("Scope", comboboxScope.Active);
//			properties.Set ("SearchPathRecursively", checkbuttonRecursively.Active);
//			properties.Set ("UseFileMask", checkbuttonFileMask.Active);
			properties.Set ("CaseSensitive", checkbuttonCaseSensitive.Active);
			properties.Set ("WholeWordsOnly", checkbuttonWholeWordsOnly.Active);
			properties.Set ("RegexSearch", checkbuttonRegexSearch.Active);

			StoreHistory ("MonoDevelop.FindReplaceDialogs.FindHistory", comboboxentryFind);
			if (showReplace)
				StoreHistory ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", comboboxentryReplace);
			properties.Set ("MonoDevelop.FindReplaceDialogs.FileMask", searchentry1.Query);
//			StoreHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
			//StoreHistory ("MonoDevelop.FindReplaceDialogs.FileMaskHistory", comboboxentryFileMask);
		}

		static void StoreHistory (string propertyName, ComboBoxEntry comboBox)
		{
			var store = (ListStore)comboBox.Model;
			var history = new List<string> ();
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					history.Add ((string)store.GetValue (iter, 0));
				} while (store.IterNext (ref iter));
			}
			const int limit = 20;
			if (history.Count > limit) {
				history.RemoveRange (history.Count - (history.Count - limit), history.Count - limit);
			}
			if (history.Contains (comboBox.Entry.Text))
				history.Remove (comboBox.Entry.Text);
			history.Insert (0, comboBox.Entry.Text);
			PropertyService.Set (propertyName, string.Join (historySeparator.ToString (), history.ToArray ()));
		}

		protected override void OnDestroyed ()
		{
			StorePoperties ();
			base.OnDestroyed ();
		}
		
		static FindInFilesDialog currentFindDialog;
		static bool IsCurrentDialogClosed {
			get {
				return currentFindDialog == null || !currentFindDialog.Visible;
			}
		}
		
		public static void ShowFind ()
		{
			if (!IsCurrentDialogClosed) {
				currentFindDialog.Destroy ();
			}
			currentFindDialog = new FindInFilesDialog (false);
			MessageService.PlaceDialog (currentFindDialog, null);
			currentFindDialog.Show ();
		}
		
		public static void ShowReplace ()
		{
			if (!IsCurrentDialogClosed) {
				currentFindDialog.Destroy ();
			}
			currentFindDialog = new FindInFilesDialog (true);
			MessageService.PlaceDialog (currentFindDialog, null);
			currentFindDialog.Show ();
		}
		
		public static void FindInPath (string path)
		{
			if (!IsCurrentDialogClosed) {
				currentFindDialog.Destroy ();
			}
			currentFindDialog = new FindInFilesDialog (false, path);
			MessageService.PlaceDialog (currentFindDialog, null);
			currentFindDialog.Show ();
		}
				
		Scope GetScope ()
		{
			switch (comboboxScope.Active) {
			case ScopeCurrentDocument:
				return new DocumentScope ();
			case ScopeSelection:
				return new SelectionScope ();
			case ScopeWholeSolution:
				if (IdeApp.ProjectOperations.CurrentSelectedSolution == null) {
					MessageService.ShowError (GettextCatalog.GetString ("Currently there is no open solution."));
					return null;
				}
				return new WholeSolutionScope ();
			case ScopeCurrentProject:
				MonoDevelop.Projects.Project currentSelectedProject = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (currentSelectedProject != null)
					return new WholeProjectScope(currentSelectedProject);
				if (IdeApp.ProjectOperations.CurrentSelectedSolution != null) {
					AlertButton alertButton = MessageService.AskQuestion (GettextCatalog.GetString ("Currently there is no project selected. Search in the solution instead ?"), AlertButton.Yes, AlertButton.No);
					if (alertButton == AlertButton.Yes)
						return new WholeSolutionScope ();
				} else {
					MessageService.ShowError (GettextCatalog.GetString ("Currently there is no open solution."));
				}
				return null;
			case ScopeAllOpenFiles:
				return new AllOpenFilesScope ();
			case ScopeDirectories: 
				if (!System.IO.Directory.Exists (comboboxentryPath.Entry.Text)) {
					MessageService.ShowError (string.Format (GettextCatalog.GetString ("Directory not found: {0}"), comboboxentryPath.Entry.Text));
					return null;
				}
				var directoryScope = new DirectoryScope (comboboxentryPath.Entry.Text, checkbuttonRecursively.Active);
				
				var properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
				directoryScope.IncludeBinaryFiles = properties.Get ("IncludeBinaryFiles", false);
				directoryScope.IncludeHiddenFiles = properties.Get ("IncludeHiddenFiles", false);
				
				return directoryScope;
			}
			throw new ApplicationException ("Unknown scope:" + comboboxScope.Active);
		}

		FilterOptions GetFilterOptions ()
		{
			return new FilterOptions {
				FileMask = !string.IsNullOrEmpty (searchentry1.Query) ? searchentry1.Query : "*",
				CaseSensitive = checkbuttonCaseSensitive.Active,
				RegexSearch = checkbuttonRegexSearch.Active,
				WholeWordsOnly = checkbuttonWholeWordsOnly.Active
			};
		}

		static FindReplace find;
		void HandleReplaceClicked (object sender, EventArgs e)
		{
			SearchReplace (comboboxentryReplace.Entry.Text);
//			Hide ();
		}

		void HandleSearchClicked (object sender, EventArgs e)
		{
			SearchReplace (null);
//			Hide ();
		}

		readonly List<ISearchProgressMonitor> searchesInProgress = new List<ISearchProgressMonitor> ();
		void UpdateStopButton ()
		{
			buttonStop.Sensitive = searchesInProgress.Count > 0;
		}

		void ButtonStopClicked (object sender, EventArgs e)
		{
			lock (searchesInProgress) {
				if (searchesInProgress.Count == 0)
					return;
				ISearchProgressMonitor monitor = searchesInProgress[searchesInProgress.Count - 1];
				monitor.AsyncOperation.Cancel ();
			}
		}

		void SearchReplace (string replacePattern)
		{
			if (find != null && find.IsRunning) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("There is a search already in progress. Do you want to stop it?"), AlertButton.Stop))
					return;
				lock (searchesInProgress) {
					foreach (var mon in searchesInProgress)
						mon.AsyncOperation.Cancel ();
					searchesInProgress.Clear ();
				}
			}
			
			Scope scope = GetScope ();
			if (scope == null)
				return;
			
			ISearchProgressMonitor searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true);
			lock (searchesInProgress)
				searchesInProgress.Add (searchMonitor);
			UpdateStopButton ();
			find = new FindReplace ();
			
			string pattern = comboboxentryFind.Entry.Text;
			FilterOptions options = GetFilterOptions ();
			searchMonitor.ReportStatus (scope.GetDescription (options, pattern, null));

			if (!find.ValidatePattern (options, pattern)) {
				MessageService.ShowError (GettextCatalog.GetString ("Search pattern is invalid"));
				return;
			}

			if (replacePattern != null && !find.ValidatePattern (options, replacePattern)) {
				MessageService.ShowError (GettextCatalog.GetString ("Replace pattern is invalid"));
				return;
			}

			DateTime timer = DateTime.Now;
			string errorMessage = null;
				
			try {
				var results = new List<SearchResult> ();
				foreach (SearchResult result in find.FindAll (scope, searchMonitor, pattern, replacePattern, options)) {
					if (searchMonitor.IsCancelRequested)
						return;
					results.Add (result);
				}
				searchMonitor.ReportResults (results);
			} catch (Exception ex) {
				errorMessage = ex.Message;
				LoggingService.LogError ("Error while search", ex);
			}
				
			string message;
			if (errorMessage != null) {
				message = GettextCatalog.GetString ("The search could not be finished: {0}", errorMessage);
				searchMonitor.ReportError (message, null);
			} else if (searchMonitor.IsCancelRequested) {
				message = GettextCatalog.GetString ("Search cancelled.");
				searchMonitor.ReportWarning (message);
			} else {
				string matches = string.Format (GettextCatalog.GetPluralString ("{0} match found", "{0} matches found", find.FoundMatchesCount), find.FoundMatchesCount);
				string files = string.Format (GettextCatalog.GetPluralString ("in {0} file.", "in {0} files.", find.SearchedFilesCount), find.SearchedFilesCount);
				message = GettextCatalog.GetString ("Search completed.") + Environment.NewLine + matches + " " + files;
				searchMonitor.ReportSuccess (message);
			}
			searchMonitor.ReportStatus (message);
			searchMonitor.Log.WriteLine (GettextCatalog.GetString ("Search time: {0} seconds."), (DateTime.Now - timer).TotalSeconds);
			searchMonitor.Dispose ();
			searchesInProgress.Remove (searchMonitor);
			UpdateStopButton ();
		}
	}
}
