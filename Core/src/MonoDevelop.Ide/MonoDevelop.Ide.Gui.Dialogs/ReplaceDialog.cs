//  ReplaceDialog.cs
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
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Gui;

using Gtk;
using Glade;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class ReplaceDialog: Gtk.Dialog
	{
		private const int historyLimit = 20;
		private const char historySeparator = '\n';
		// regular members
		public bool replaceMode;
		StringCollection findHistory = new StringCollection();
		StringCollection replaceHistory = new StringCollection();
		
		// services

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
			combos.AddWidget(searchPatternEntry);
			helpButtons.AddWidget(findHelpButton);
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
  			
 			searchPatternEntry.Model = new ListStore(typeof(string));
 			searchPatternEntry.TextColumn = 0;
			
			// set button sensitivity
			findHelpButton.Sensitive = false;
			
			// set replace dialog properties 
			if (replaceMode)
			{
				markAllButton.Visible = false;
				replacePatternEntry.Entry.Completion = new EntryCompletion ();
				replacePatternEntry.Entry.Completion.Model = new ListStore (typeof (string));
				replacePatternEntry.Entry.Completion.TextColumn = 0;
				replacePatternEntry.Entry.ActivatesDefault = true;
				
				replacePatternEntry.Model = new ListStore(typeof(string));
				replacePatternEntry.TextColumn = 0;

				// set the label properties
				replaceButton.UseUnderline = true;
				replaceAllButton.UseUnderline = true;
				
				// set te size groups to include the replace dialog
				labels.AddWidget(labelReplace);
				combos.AddWidget(replacePatternEntry);
				helpButtons.AddWidget(replaceHelpButton);
				
				replaceHelpButton.Sensitive = false;
			}
			else
			{
				labelReplace.Visible = replacePatternEntry.Visible = false;
				replaceAllButton.Visible = replaceHelpButton.Visible = replaceButton.Visible = false;
				markAllButton.UseUnderline = true;
			}
			this.Resize (500, 200);
			TransientFor = IdeApp.Workbench.RootWindow;
		}
		
		public ReplaceDialog (bool replaceMode)
		{
			Build ();
			
			// some members needed to initialise this dialog based on replace mode
			this.replaceMode = replaceMode;
			
			InitDialog ();
			
			LoadHistoryValues();
			
			ignoreCaseCheckBox.Active = !SearchReplaceManager.SearchOptions.IgnoreCase;
			searchWholeWordOnlyCheckBox.Active = SearchReplaceManager.SearchOptions.SearchWholeWordOnly;
			
			useSpecialSearchStrategyCheckBox.Active  = SearchReplaceManager.SearchOptions.SearchStrategyType != SearchStrategyType.Normal;
			useSpecialSearchStrategyCheckBox.Toggled += new EventHandler(SpecialSearchStrategyCheckBoxChangedEvent);
			
			ListStore store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Wildcards"));
			store.AppendValues (GettextCatalog.GetString ("Regular Expressions"));
			specialSearchStrategyComboBox.Model = store;

			CellRendererText cr = new CellRendererText ();
			specialSearchStrategyComboBox.PackStart (cr, true);
			specialSearchStrategyComboBox.AddAttribute (cr, "text", 0);
			
			int index = 0;
			switch (SearchReplaceManager.SearchOptions.SearchStrategyType) {
				case SearchStrategyType.Normal:
				case SearchStrategyType.Wildcard:
					searchWholeWordOnlyCheckBox.Sensitive = true;
					break;
				case SearchStrategyType.RegEx:
					searchWholeWordOnlyCheckBox.Sensitive = false;
					index = 1;
					break;
			}
			specialSearchStrategyComboBox.Active = index;
			specialSearchStrategyComboBox.Changed += new EventHandler (OnSpecialSearchStrategyChanged);
			
			store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Current File"));
			store.AppendValues (GettextCatalog.GetString ("All Open Files"));
			
			searchLocationComboBox.Model = store;
			searchLocationComboBox.PackStart (cr, true);
			searchLocationComboBox.AddAttribute (cr, "text", 0);
			
			index = 0;
			if (SearchReplaceManager.SearchOptions.DocumentIteratorType == DocumentIteratorType.AllOpenFiles)
				index = 1;
			
			searchLocationComboBox.Active = index;
			
			searchPatternEntry.Entry.Text = SearchReplaceManager.SearchOptions.SearchPattern;
			
			// insert event handlers
			findButton.Clicked  += new EventHandler(FindNextEvent);
			closeButton.Clicked += new EventHandler(CloseDialogEvent);
			Close += new EventHandler(CloseDialogEvent);
			DeleteEvent += new DeleteEventHandler (OnDeleted);
			
			if (replaceMode) {
				Title = GettextCatalog.GetString ("Replace");
				replaceButton.Clicked    += new EventHandler(ReplaceEvent);
				replaceAllButton.Clicked += new EventHandler(ReplaceAllEvent);
				replacePatternEntry.Entry.Text = SearchReplaceManager.SearchOptions.ReplacePattern;
			} else {
				Title = GettextCatalog.GetString ("Find");
				markAllButton.Clicked    += new EventHandler(MarkAllEvent);
			}
			searchPatternEntry.Entry.SelectRegion(0, searchPatternEntry.ActiveText.Length);
			
			SpecialSearchStrategyCheckBoxChangedEvent(null, null);
			SearchReplaceManager.ReplaceDialog     = this;
		}
		
		protected void OnClosed()
		{
			SaveHistoryValues();
			SearchReplaceManager.ReplaceDialog = null;
		}
		
		void OnDeleted (object o, DeleteEventArgs args)
		{
			// perform the standard closing windows event
			OnClosed();
			SearchReplaceManager.ReplaceDialog = null;
		}

		public void SetSearchPattern(string pattern)
		{
			searchPatternEntry.Entry.Text = pattern;
		}

		void OnSpecialSearchStrategyChanged (object o, EventArgs e)
		{
			if (specialSearchStrategyComboBox.Active != 1) {
				searchWholeWordOnlyCheckBox.Sensitive = true;
			} else {
				searchWholeWordOnlyCheckBox.Sensitive = false;
			}
		}
		
		void SetupSearchReplaceManager()
		{
			SearchReplaceManager.SearchOptions.SearchPattern  = searchPatternEntry.ActiveText;
			if (replaceMode) {
				SearchReplaceManager.SearchOptions.ReplacePattern = replacePatternEntry.ActiveText;
			}
			
			SearchReplaceManager.SearchOptions.IgnoreCase          = !ignoreCaseCheckBox.Active;
			SearchReplaceManager.SearchOptions.SearchWholeWordOnly = searchWholeWordOnlyCheckBox.Active;
			
			if (useSpecialSearchStrategyCheckBox.Active) {
				switch (specialSearchStrategyComboBox.Active) {
					case 0:
						SearchReplaceManager.SearchOptions.SearchStrategyType = SearchStrategyType.Wildcard;
						break;
					case 1:
						SearchReplaceManager.SearchOptions.SearchStrategyType = SearchStrategyType.RegEx;
						break;
				}
			} else {
				SearchReplaceManager.SearchOptions.SearchStrategyType = SearchStrategyType.Normal;
			}
			
			switch (searchLocationComboBox.Active) {
				case 0:
					SearchReplaceManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.CurrentDocument;
					break;
				case 1:
					SearchReplaceManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.AllOpenFiles;
					break;
			}
		}
		
		void FindNextEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.ActiveText.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.FindNext();
			
			AddSearchHistoryItem(findHistory, searchPatternEntry.ActiveText);
		}
		
		void ReplaceEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.ActiveText.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.Replace();
			
			AddSearchHistoryItem(replaceHistory, replacePatternEntry.ActiveText);
		}
		
		void ReplaceAllEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.ActiveText.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.ReplaceAll();
			
			AddSearchHistoryItem(replaceHistory, replacePatternEntry.ActiveText);
		}
		
		void MarkAllEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.ActiveText.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.MarkAll();			
			
			AddSearchHistoryItem(findHistory, searchPatternEntry.ActiveText);
		}
		
		void CloseDialogEvent(object sender, EventArgs e)
		{
			Hide();
			OnClosed ();
		}
		
		void SpecialSearchStrategyCheckBoxChangedEvent(object sender, EventArgs e)
		{
			if (useSpecialSearchStrategyCheckBox != null) {
				specialSearchStrategyComboBox.Sensitive = useSpecialSearchStrategyCheckBox.Active;
				if (useSpecialSearchStrategyCheckBox.Active) {
					if (specialSearchStrategyComboBox.Active == 1) {
						searchWholeWordOnlyCheckBox.Sensitive = false;
					}
				} else {
					searchWholeWordOnlyCheckBox.Sensitive = true;
				}
			}
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
			object stringArray;
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
			
			// now do the replace history
			if (replaceMode)	{
				stringArray = new string[replaceHistory.Count];
				replaceHistory.CopyTo (stringArray, 0);				
				PropertyService.Set ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", string.Join(historySeparator.ToString(), stringArray));
			}
		}
		
		public new void Destroy()
		{
			// save the search and replace history to properties
			OnClosed ();
			base.Destroy();
		}
		
		public new void ShowAll()
		{
			base.Show();
			searchPatternEntry.Entry.SelectRegion (0, searchPatternEntry.ActiveText.Length);
		}
	}
}
