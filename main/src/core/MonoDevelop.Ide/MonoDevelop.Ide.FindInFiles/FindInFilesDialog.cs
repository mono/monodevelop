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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.FindInFiles
{
	public partial class FindInFilesDialog : Gtk.Dialog
	{
		bool writeScope = true;
		bool showReplace;
		
		public FindInFilesDialog (bool showReplace, string directory) : this (showReplace)
		{
			comboboxScope.Active = 3;
			comboboxentryPath.Entry.Text = directory;
			CheckSensitivity (this, EventArgs.Empty);
			writeScope = false;
		}
		ComboBoxEntry comboboxentryReplace;
		Label labelReplace;
		public FindInFilesDialog (bool showReplace)
		{
			this.showReplace = showReplace;
			this.Build();
			this.Title = showReplace ? GettextCatalog.GetString ("Replace in Files") : GettextCatalog.GetString ("Find in Files");
			
			if (!showReplace) {
				buttonReplace.Destroy ();
			}
			
			if (showReplace) {
				tableFindAndReplace.NRows = 3;
				labelReplace = new Label ();
				labelReplace.Text = GettextCatalog.GetString ("_Replace");
				labelReplace.UseUnderline = true;
				tableFindAndReplace.Add (labelReplace);
				
				comboboxentryReplace = new ComboBoxEntry ();
				tableFindAndReplace.Add (comboboxentryReplace);
				
				Gtk.Table.TableChild childLabel = (Gtk.Table.TableChild)this.tableFindAndReplace[this.labelReplace];
				childLabel.TopAttach = 1;
				childLabel.BottomAttach = 2;
				childLabel.XOptions = childLabel.YOptions = (Gtk.AttachOptions)4;
				
				Gtk.Table.TableChild childCombo = (Gtk.Table.TableChild)this.tableFindAndReplace[this.comboboxentryReplace];
				childCombo.TopAttach = 1;
				childCombo.BottomAttach = 2;
				childCombo.LeftAttach = 1;
				childCombo.RightAttach = 2;
				childCombo.XOptions = childCombo.YOptions = (Gtk.AttachOptions)4;
				
				childLabel = (Gtk.Table.TableChild)this.tableFindAndReplace[this.labelScope];
				childLabel.TopAttach = 2;
				childLabel.BottomAttach = 3;
				childLabel.XOptions = childLabel.YOptions = (Gtk.AttachOptions)4;
				
				childCombo = (Gtk.Table.TableChild)this.tableFindAndReplace[this.comboboxScope];
				childCombo.TopAttach = 2;
				childCombo.BottomAttach = 3;
				childCombo.LeftAttach = 1;
				childCombo.RightAttach = 2;
				childCombo.XOptions = childCombo.YOptions = (Gtk.AttachOptions)4;
				
				ShowAll ();
			}
			
			checkbuttonFileMask.Toggled += CheckSensitivity;
			buttonReplace.Clicked += HandleReplaceClicked;
			buttonSearch.Clicked += HandleSearchClicked;
			
			ListStore scopeStore = new ListStore (typeof(string));
			scopeStore.AppendValues (GettextCatalog.GetString ("Whole solution"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Current project"));
			scopeStore.AppendValues (GettextCatalog.GetString ("All open files"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Directories"));
			comboboxScope.Model = scopeStore;
			CellRendererText textRenderer = new CellRendererText ();
			
			comboboxScope.Changed += HandleScopeChanged;
			comboboxScope.Changed += CheckSensitivity;
			
			InitFromProperties ();
			CheckSensitivity (this, EventArgs.Empty);
			
			if (IdeApp.Workbench.ActiveDocument != null) {
				ITextBuffer view = IdeApp.Workbench.ActiveDocument.GetContent<ITextBuffer> (); 
				if (view != null) {
					string selectedText = view.SelectedText;
					if (!string.IsNullOrEmpty (selectedText))
						comboboxentryFind.Entry.Text =selectedText;
				}
			}
			comboboxentryFind.Entry.SelectRegion (0, comboboxentryFind.ActiveText.Length);
			
		}
		
		ComboBoxEntry comboboxentryPath;
		Button        buttonBrowsePaths;
		CheckButton   checkbuttonRecursively;
		void HandleScopeChanged (object sender, EventArgs e)
		{
			while (boxScopeSelector.Children.Length > 0) {
				Widget w = boxScopeSelector.Children [0];
				boxScopeSelector.Remove (w);
				w.Destroy ();
			}
			comboboxentryPath = null;
			buttonBrowsePaths = null;
			checkbuttonRecursively = null;
			if (comboboxScope.Active != 3) {
				Button placeHolder = new Button ();
				boxScopeSelector.PackEnd (placeHolder);
				ShowAll ();
				placeHolder.Hide ();
			}
			if (comboboxScope.Active == 3) { // DirectoryScope
				Properties properties = (Properties)PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
				comboboxentryPath = new ComboBoxEntry ();
				comboboxentryPath.Destroyed += delegate(object sender2, EventArgs e2) {
					StoreHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", (ComboBoxEntry)sender2);
				};
				LoadHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
				boxScopeSelector.PackStart (comboboxentryPath);
				Gtk.Box.BoxChild boxChild = (Gtk.Box.BoxChild)boxScopeSelector[comboboxentryPath];
				boxChild.Position = 0;
				boxChild.Expand = boxChild.Fill = true;
				
				buttonBrowsePaths = new Button ();
				buttonBrowsePaths.Label = "...";
				buttonBrowsePaths.Clicked += delegate {
					FolderDialog folderDialog = new FolderDialog (GettextCatalog.GetString ("Select directory"));
					try {
						string defaultFolder = this.comboboxentryFind.Entry.Text;	
						if (string.IsNullOrEmpty (defaultFolder)) 
							defaultFolder = IdeApp.ProjectOperations.ProjectsDefaultPath;
						
						folderDialog.SetFilename (defaultFolder);
						if (folderDialog.Run() == (int)Gtk.ResponseType.Ok) 
							this.comboboxentryPath.Entry.Text = folderDialog.Filename;
					} finally {
						folderDialog.Destroy ();
					}
				};
				boxScopeSelector.PackStart (buttonBrowsePaths);
				boxChild = (Gtk.Box.BoxChild)boxScopeSelector[buttonBrowsePaths];
				boxChild.Position = 1;
				boxChild.Expand = boxChild.Fill = false;
				
				checkbuttonRecursively = new CheckButton ();
				checkbuttonRecursively.Label = GettextCatalog.GetString ("Re_cursively");
				checkbuttonRecursively.Active = properties.Get ("SearchPathRecursively", true);
				checkbuttonRecursively.UseUnderline = true;
				checkbuttonRecursively.Destroyed += delegate(object sender2, EventArgs e2) {
					properties.Set ("SearchPathRecursively", ((CheckButton)sender2).Active);
				};
				boxScopeSelector.PackEnd (checkbuttonRecursively);
				boxChild = (Gtk.Box.BoxChild)boxScopeSelector[checkbuttonRecursively];
				boxChild.Position = 2;
				boxChild.Expand = boxChild.Fill = false;
				
				ShowAll ();
			}
		}
		
		const char historySeparator = '\n';
		void InitFromProperties ()
		{
			Properties properties = (Properties)PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
			comboboxScope.Active = properties.Get ("Scope", 0);
			
			//checkbuttonRecursively.Active    = properties.Get ("SearchPathRecursively", true);
			checkbuttonFileMask.Active       = properties.Get ("UseFileMask", false);
			checkbuttonCaseSensitive.Active  = properties.Get ("CaseSensitive", true);
			checkbuttonWholeWordsOnly.Active = properties.Get ("WholeWordsOnly", false);
			checkbuttonRegexSearch.Active    = properties.Get ("RegexSearch", false);
			
			LoadHistory ("MonoDevelop.FindReplaceDialogs.FindHistory", comboboxentryFind);
			if (showReplace)
				LoadHistory ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", comboboxentryReplace);
//			LoadHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
			LoadHistory ("MonoDevelop.FindReplaceDialogs.FileMaskHistory", comboboxentryFileMask);
		}
		
		static void LoadHistory (string propertyName, ComboBoxEntry entry)
		{
			entry.Entry.Completion = new EntryCompletion ();
			
			ListStore store = new ListStore (typeof (string));
			
			entry.Entry.Completion.Model = store;
			entry.Model = store;
			
			entry.Entry.ActivatesDefault = true;
			
			string history = PropertyService.Get<string> (propertyName);
			if (!string.IsNullOrEmpty (history)) {
				string[] items = history.Split (historySeparator);
				foreach (string item in items) {
					store.AppendValues (item);
				}
				entry.Entry.Text = items[0];
			}
		}
		
		void StorePoperties ()
		{
			Properties properties = (Properties)PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
			if (writeScope)
				properties.Set ("Scope", comboboxScope.Active);
//			properties.Set ("SearchPathRecursively", checkbuttonRecursively.Active);
			properties.Set ("UseFileMask", checkbuttonFileMask.Active);
			properties.Set ("CaseSensitive", checkbuttonCaseSensitive.Active);
			properties.Set ("WholeWordsOnly", checkbuttonWholeWordsOnly.Active);
			properties.Set ("RegexSearch", checkbuttonRegexSearch.Active);
			
			StoreHistory ("MonoDevelop.FindReplaceDialogs.FindHistory", comboboxentryFind);
			if (showReplace)
				StoreHistory ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", comboboxentryReplace);
//			StoreHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
			StoreHistory ("MonoDevelop.FindReplaceDialogs.FileMaskHistory", comboboxentryFileMask);
		}
		
		static void StoreHistory (string propertyName, Gtk.ComboBoxEntry comboBox)
		{
			ListStore store = (ListStore)comboBox.Model;
			List<string> history = new List<string> ();
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
		
		public static void FindInPath (string path)
		{
			FindInFilesDialog findInFilesDialog = new FindInFilesDialog (false, path);
			findInFilesDialog.Run ();
			findInFilesDialog.Destroy ();
		}
		
		void CheckSensitivity (object sender, EventArgs args)
		{
			comboboxentryFileMask.Sensitive = checkbuttonFileMask.Active;
			//comboboxentryProject.Sensitive = radiobuttonProject.Active;
			//comboboxentryPath.Sensitive = buttonBrowsePaths.Sensitive = checkbuttonRecursively.Sensitive = radiobuttonDirectory.Active;
		}
		
		Scope GetScope ()
		{
			switch (comboboxScope.Active) {
			case 0:
				return new WholeSolutionScope ();
			case 1:
				return new WholeProjectScope (IdeApp.ProjectOperations.CurrentSelectedProject);
			case 2:
				return new AllOpenFilesScope ();
			case 3:
				return new DirectoryScope (comboboxentryPath.Entry.Text, checkbuttonRecursively.Active);
			}
			throw new ApplicationException ("Unknown scope:" + comboboxScope.Active);
		}
		
		FilterOptions GetFilterOptions ()
		{
			FilterOptions result = new FilterOptions ();
			result.FileMask = checkbuttonFileMask.Active ? comboboxentryFileMask.Entry.Text : "*";
			result.CaseSensitive = checkbuttonCaseSensitive.Active;
			result.RegexSearch = checkbuttonRegexSearch.Active;
			result.WholeWordsOnly = checkbuttonWholeWordsOnly.Active;
			return result;
		}
		
		static FindReplace find;
		bool isCanceled = false;
		
		void HandleReplaceClicked (object sender, EventArgs e)
		{
			SearchReplace (comboboxentryReplace.Entry.Text);
		}
		
		void HandleSearchClicked (object sender, EventArgs e)
		{
			SearchReplace (null);
		}
		
		void SearchReplace (string replacePattern) 
		{
			if (find != null && find.IsRunning) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("There is a search already in progress. Do you want to stop it?"), AlertButton.Stop))
					return;
				CancelSearch ();
			}
			
			ISearchProgressMonitor searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true);
			searchMonitor.CancelRequested += (MonitorHandler) DispatchService.GuiDispatch (new MonitorHandler (OnCancelRequested));
			
			find                  = new FindReplace ();
			Scope         scope   = GetScope ();
			string        pattern = comboboxentryFind.Entry.Text;
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
			
			DispatchService.BackgroundDispatch (delegate {
				DateTime timer = DateTime.Now;
				string errorMessage = null;
				try {
					foreach (SearchResult result in find.FindAll (scope, pattern, replacePattern, options)) {
						searchMonitor.ReportResult (result);
					}
				} catch (Exception ex) {
					errorMessage = ex.Message;
					LoggingService.LogError ("Error while search", ex);
				}
				Application.Invoke (delegate {
					string message;
					if (errorMessage != null) {
						message = GettextCatalog.GetString ("The search could not be finished: {0}", errorMessage);
					} else if (isCanceled) {
						message = GettextCatalog.GetString ("Search cancelled.");
					} else {
						string matches = string.Format(GettextCatalog.GetPluralString("{0} match found ", "{0} matches found ", find.FoundMatchesCount), find.FoundMatchesCount);
						string files   = string.Format(GettextCatalog.GetPluralString("in {0} file.", "in {0} files.", find.SearchedFilesCount), find.SearchedFilesCount);
						message = GettextCatalog.GetString("Search completed. ") + 
						          matches + 
						          files;
					}
					
					searchMonitor.ReportStatus (message);
					searchMonitor.Log.WriteLine (message);
					searchMonitor.Log.WriteLine (GettextCatalog.GetString ("Search time: {0} seconds."), (DateTime.Now - timer).TotalSeconds);
					searchMonitor.Dispose ();
				});
			});
		}
		
		void OnCancelRequested (IProgressMonitor monitor)
		{
			CancelSearch ();
		}
		
		public void CancelSearch ()
		{
			isCanceled = true;
			if (find != null) 
				find.IsCanceled = true;
		}
	}
}
