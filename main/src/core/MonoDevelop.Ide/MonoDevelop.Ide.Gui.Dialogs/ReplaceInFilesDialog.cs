//  ReplaceInFilesDialog.cs
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
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Specialized;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Gui;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class ReplaceInFilesDialog: Gtk.Dialog
	{
 		public bool replaceMode;
 		private const int historyLimit = 20;
 		private const char historySeparator = '\n';
 		StringCollection findHistory = new StringCollection();
 		StringCollection replaceHistory = new StringCollection();
 		

		void InitDialog ()
		{
			findButton.UseUnderline = true;			
			closeButton.UseUnderline = true;
			
			//set up the size groups
			SizeGroup labels = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup combos = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup options = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup helpButtons = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup checkButtons = new SizeGroup(SizeGroupMode.Horizontal);
			labels.AddWidget(label1);
			labels.AddWidget(label6);
			labels.AddWidget(label7);
			combos.AddWidget(searchPatternEntry);
			combos.AddWidget(directoryTextBox);
			combos.AddWidget(fileMaskTextBox);
			helpButtons.AddWidget(findHelpButton);
			helpButtons.AddWidget(browseButton);
			checkButtons.AddWidget (includeSubdirectoriesCheckBox);
			checkButtons.AddWidget(ignoreCaseCheckBox);
			checkButtons.AddWidget(searchWholeWordOnlyCheckBox);
			checkButtons.AddWidget(useSpecialSearchStrategyCheckBox);
			checkButtons.AddWidget(searchLocationLabel);
			options.AddWidget(specialSearchStrategyComboBox);
			options.AddWidget(searchLocationComboBox);

			searchPatternEntry.Entry.Completion = new EntryCompletion ();
			searchPatternEntry.Entry.Completion.Model = new ListStore (typeof (string));
			searchPatternEntry.Entry.Completion.TextColumn = 0;
			searchPatternEntry.Entry.ActivatesDefault = true;
			
			searchPatternEntry.Model = new ListStore(typeof (string));
			searchPatternEntry.TextColumn = 0;
			
			// set button sensitivity
			findHelpButton.Sensitive = false;
			
			// set replace dialog properties 
			if (replaceMode)
			{
				replacePatternEntry.Entry.Completion = new EntryCompletion ();
				replacePatternEntry.Entry.Completion.Model = new ListStore (typeof (string));
				replacePatternEntry.Entry.Completion.TextColumn = 0;
				replacePatternEntry.Entry.ActivatesDefault = true;
				
				replacePatternEntry.Model = new ListStore(typeof (string));
				replacePatternEntry.TextColumn = 0;

				// set the size groups to include the replace dialog
				labels.AddWidget(labelReplace);
				combos.AddWidget(replacePatternEntry);
				helpButtons.AddWidget(replaceHelpButton);
				
				replaceHelpButton.Sensitive = false;
			} else {
				Title = GettextCatalog.GetString ("Find in Files");
				labelReplace.Visible = replaceAllButton.Visible = false;
				replacePatternEntry.Visible = replaceHelpButton.Visible = false;
				
			}
			this.Resize (500, 200);
			TransientFor = IdeApp.Workbench.RootWindow;
		}

		protected void OnClosed()
		{
			SaveHistoryValues();
		}
		
		void OnDeleted (object o, DeleteEventArgs args)
		{
			// perform the standard closing windows event
			OnClosed();
			SearchReplaceInFilesManager.ReplaceDialog = null;
		}

		void CloseDialogEvent(object sender, EventArgs e)
		{
			Hide();
			OnClosed ();
		}

		public new void ShowAll ()
		{
			base.ShowAll ();
			SearchReplaceInFilesManager.ReplaceDialog = this;
			searchPatternEntry.Entry.SelectRegion (0, searchPatternEntry.ActiveText.Length);
		}

		public ReplaceInFilesDialog (bool replaceMode)
		{
			Build ();
			
			this.replaceMode = replaceMode;
			
			InitDialog ();
			LoadHistoryValues();

			CellRendererText cr = new CellRendererText ();
			Gtk.ListStore store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Wildcards"));
			store.AppendValues (GettextCatalog.GetString ("Regular Expressions"));
			specialSearchStrategyComboBox.Model = store;
			specialSearchStrategyComboBox.PackStart (cr, true);
			specialSearchStrategyComboBox.AddAttribute (cr, "text", 0);
			
			specialSearchStrategyComboBox.Changed += new EventHandler (OnSpecialSearchStrategyChanged);
			
			store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Directories"));
			store.AppendValues (GettextCatalog.GetString ("All open files"));
			store.AppendValues (GettextCatalog.GetString ("Whole solution"));
			searchLocationComboBox.Model = store;
			searchLocationComboBox.PackStart (cr, true);
			searchLocationComboBox.AddAttribute (cr, "text", 0);
			
			LoadOptions ();

			searchLocationComboBox.Changed += new EventHandler(SearchLocationCheckBoxChangedEvent);
			useSpecialSearchStrategyCheckBox.Toggled += new EventHandler(SpecialSearchStrategyCheckBoxChangedEvent);
			
			browseButton.Clicked += new EventHandler(BrowseDirectoryEvent);
			findButton.Clicked += new EventHandler(FindEvent);

			stopButton.Clicked += new EventHandler(StopEvent);
			stopButton.Sensitive = false;
			
			if (replaceMode) {
				replaceAllButton.Clicked += new EventHandler(ReplaceEvent);
			}
			
			Close += new EventHandler (CloseDialogEvent);
			closeButton.Clicked += new EventHandler (CloseDialogEvent);
			DeleteEvent += new DeleteEventHandler (OnDeleted);
			searchPatternEntry.Entry.SelectRegion (0, searchPatternEntry.ActiveText.Length);
			
			SearchLocationCheckBoxChangedEvent (null, null);
			SpecialSearchStrategyCheckBoxChangedEvent (null, null);
		}
		
		public void LoadOptions ()
		{
			int index = 0;
			switch (SearchReplaceManager.SearchOptions.SearchStrategyType) {
				case SearchStrategyType.Normal:
				case SearchStrategyType.Wildcard:
					break;
				case SearchStrategyType.RegEx:
					searchWholeWordOnlyCheckBox.Sensitive = false;
					index = 1;
					break;
			}
			specialSearchStrategyComboBox.Active = index;
			
			index = 0;
			switch (SearchReplaceInFilesManager.SearchOptions.DocumentIteratorType) {
				case DocumentIteratorType.AllOpenFiles:
					index = 1;
					break;
				case DocumentIteratorType.WholeCombine:
					index = 2;
					break;
			}
			
			searchLocationComboBox.Active = index;
			
			directoryTextBox.Text = SearchReplaceInFilesManager.SearchOptions.SearchDirectory;
			fileMaskTextBox.Text = SearchReplaceInFilesManager.SearchOptions.FileMask;
			includeSubdirectoriesCheckBox.Active = SearchReplaceInFilesManager.SearchOptions.SearchSubdirectories;
			
			searchLocationComboBox.Active = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.DocumentIterator", 0);
			
			searchPatternEntry.Entry.Text = SearchReplaceInFilesManager.SearchOptions.SearchPattern;
			
			if (replacePatternEntry != null)
				replacePatternEntry.Entry.Text = SearchReplaceInFilesManager.SearchOptions.ReplacePattern;
		}
		
		void FindEvent (object sender, EventArgs e)
		{
			if (SetupSearchReplaceInFilesManager ()) {
				stopButton.Sensitive = true;
				SearchReplaceInFilesManager.NextSearchFinished += delegate {
					stopButton.Sensitive = false;
				};

				SearchReplaceInFilesManager.FindAll ();
			}
			AddSearchHistoryItem(findHistory, searchPatternEntry.ActiveText);
		}

		void StopEvent (object sender, EventArgs e)
		{
			SearchReplaceInFilesManager.CancelSearch();
		}

		void OnSpecialSearchStrategyChanged (object o, EventArgs e)
		{
			if (specialSearchStrategyComboBox.Active != 1) {
				searchWholeWordOnlyCheckBox.Sensitive = true;
			} else {
				searchWholeWordOnlyCheckBox.Sensitive = false;
			}
		}
		
						
		void ReplaceEvent(object sender, EventArgs e)
		{
			if (SetupSearchReplaceInFilesManager ()) {
				stopButton.Sensitive = true;
				SearchReplaceInFilesManager.NextSearchFinished += delegate {
					stopButton.Sensitive = false;
				};
				SearchReplaceInFilesManager.ReplaceAll ();
			}
		}
		
		void BrowseDirectoryEvent (object sender, EventArgs e)
		{
			FolderDialog fd = new FolderDialog (GettextCatalog.GetString ("Select directory"));

			try {
				// set up the dialog to point to currently selected folder, or the default projects folder
				string defaultFolder = this.directoryTextBox.Text;	
				if (defaultFolder == string.Empty || defaultFolder == null) {
					// only use the bew project default path if there is no path set
					defaultFolder = IdeApp.ProjectOperations.ProjectsDefaultPath;
				}
				fd.SetFilename( defaultFolder );
				if (fd.Run() == (int)Gtk.ResponseType.Ok)
				{
					directoryTextBox.Text = fd.Filename;
				}
				fd.Hide ();
			} finally {
				fd.Destroy ();
			}
		}
		
		void SearchLocationCheckBoxChangedEvent(object sender, EventArgs e)
		{
			bool enableDirectorySearch = searchLocationComboBox.Active == 0;
			fileMaskTextBox.Sensitive = enableDirectorySearch;
			directoryTextBox.Sensitive = enableDirectorySearch;
			browseButton.Sensitive = enableDirectorySearch;
			includeSubdirectoriesCheckBox.Sensitive = enableDirectorySearch;
		}
		
		void SpecialSearchStrategyCheckBoxChangedEvent (object sender, EventArgs e)
		{
			specialSearchStrategyComboBox.Sensitive = useSpecialSearchStrategyCheckBox.Active;
			if (useSpecialSearchStrategyCheckBox.Active) {
				if (specialSearchStrategyComboBox.Active == 1) {
					searchWholeWordOnlyCheckBox.Sensitive = false;
				}
			} else {
				searchWholeWordOnlyCheckBox.Sensitive = true;
			}
		}
		
		public void SetSearchPattern(string pattern)
		{
			searchPatternEntry.Entry.Text  = pattern;
		}

		bool SetupSearchReplaceInFilesManager()
		{
			string directoryName = directoryTextBox.Text;
			string fileMask      = fileMaskTextBox.Text;
			string searchPattern = searchPatternEntry.ActiveText;

			if (fileMask == null || fileMask.Length == 0) {
				fileMask = "*";
			}

			if (searchPattern == string.Empty) {
				MessageService.ShowError (GettextCatalog.GetString ("Empty search pattern"));
				return false;
			}
			
			if (searchLocationComboBox.Active == 0) {
				
				if (directoryName == string.Empty) {
					MessageService.ShowError (GettextCatalog.GetString ("Empty directory name"));
					return false;
				}

				if (!FileService.IsValidFileName(directoryName)) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid directory name: {0}", directoryName));
					return false;
				}
				
				if (!Directory.Exists(directoryName)) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid directory name: {0}, directoryName"));
					return false;
				}
				
				if (!FileService.IsValidFileName(fileMask) || fileMask.IndexOf('\\') >= 0) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid file mask: {0}", fileMask));
					return false;
				}
			}

			SearchReplaceInFilesManager.SearchOptions.FileMask        = fileMask;
			SearchReplaceInFilesManager.SearchOptions.SearchDirectory = directoryName;
			SearchReplaceInFilesManager.SearchOptions.SearchSubdirectories = includeSubdirectoriesCheckBox.Active;
			
			SearchReplaceInFilesManager.SearchOptions.SearchPattern  = searchPattern;
			if (replaceMode)
				SearchReplaceInFilesManager.SearchOptions.ReplacePattern = replacePatternEntry.ActiveText;
			
			SearchReplaceInFilesManager.SearchOptions.IgnoreCase          = !ignoreCaseCheckBox.Active;
			SearchReplaceInFilesManager.SearchOptions.SearchWholeWordOnly = searchWholeWordOnlyCheckBox.Active;
			
			if (useSpecialSearchStrategyCheckBox.Active) {
				switch (specialSearchStrategyComboBox.Active) {
					case 0:
						SearchReplaceInFilesManager.SearchOptions.SearchStrategyType = SearchStrategyType.Wildcard;
						break;
					case 1:
						SearchReplaceInFilesManager.SearchOptions.SearchStrategyType = SearchStrategyType.RegEx;
						break;
				}
			} else {
				SearchReplaceInFilesManager.SearchOptions.SearchStrategyType = SearchStrategyType.Normal;
			}
			
			PropertyService.Set ("MonoDevelop.FindReplaceDialogs.DocumentIterator", searchLocationComboBox.Active);
			switch (searchLocationComboBox.Active) {
				case 0:
					SearchReplaceInFilesManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.Directory;
					break;
				case 1:
					SearchReplaceInFilesManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.AllOpenFiles;
					break;
				case 2:
					SearchReplaceInFilesManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.WholeCombine;
					break;
			}
			return true;
		}
		
		// generic method to add a string to a history item
		private void AddSearchHistoryItem (StringCollection history, string toAdd)
		{
			// add the item to the find history
			if (history.Contains(toAdd)) {
				// remove it so it gets added at the top
				history.Remove(toAdd);
			}
			// make sure there is only 20
			if (history.Count == historyLimit) {
				history.RemoveAt(historyLimit - 1);
			}
			history.Insert(0, toAdd);
			
			// update the drop down for the combobox
			ListStore store = new ListStore (typeof (string));
			for (int i = 0; i < history.Count; i ++)
				store.AppendValues (history[i]);

			if (history == findHistory) {
				searchPatternEntry.Entry.Completion.Model = store;
				searchPatternEntry.Model = store;
			}
			else if( history == replaceHistory) {
				replacePatternEntry.Entry.Completion.Model = store;
				replacePatternEntry.Model = store;
			}
		}
		
		
		// loads the history arrays from the property service
		// NOTE: a newline character separates the search history strings
		private void LoadHistoryValues()
		{
			string stringArray;
			// set the history in properties
			stringArray = PropertyService.Get<string> ("MonoDevelop.FindReplaceDialogs.FindHistory");
		
			if (stringArray != null) {
				string[] items = stringArray.ToString ().Split (historySeparator);
				ListStore store = new ListStore (typeof (string));

				if(items != null) {
					findHistory.AddRange (items);
					foreach (string i in items) {
						store.AppendValues (i);
					}
				}

				searchPatternEntry.Entry.Completion.Model = store;
				searchPatternEntry.Model = store;
			}
						
			// now do the replace history
			stringArray = PropertyService.Get<string> ("MonoDevelop.FindReplaceDialogs.ReplaceHistory");
			
			if (replaceMode) {
				if (stringArray != null) {
					string[] items = stringArray.ToString ().Split (historySeparator);
					ListStore store = new ListStore (typeof (string));
					
					if(items != null) {
						replaceHistory.AddRange (items);
						foreach (string i in items) {
							store.AppendValues (i);
						}
					}
					
					replacePatternEntry.Entry.Completion.Model = store;
					replacePatternEntry.Model = store;
				}
			}
		}
		
				
		// saves the history arrays to the property service
		// NOTE: a newline character separates the search history strings
		private void SaveHistoryValues()
		{
			string[] stringArray;
			// set the history in properties
			stringArray = new string[findHistory.Count];
			findHistory.CopyTo (stringArray, 0);			
			PropertyService.Set ("MonoDevelop.FindReplaceDialogs.FindHistory", string.Join(historySeparator.ToString(), stringArray));
			
			PropertyService.Set ("MonoDevelop.FindReplaceDialogs.FindHistory", string.Join(historySeparator.ToString(), stringArray));
			
			// now do the replace history
			if (replaceMode)	{
				stringArray = new string[replaceHistory.Count];
				replaceHistory.CopyTo (stringArray, 0);				
				PropertyService.Set ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", string.Join(historySeparator.ToString(), stringArray));
			}
		}
	}
}
