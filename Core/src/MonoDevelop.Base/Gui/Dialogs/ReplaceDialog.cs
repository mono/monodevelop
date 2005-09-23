// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Specialized;

using MonoDevelop.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Search;

using Gtk;
using Glade;

namespace MonoDevelop.Gui.Dialogs
{
	internal class ReplaceDialog
	{
		private const int historyLimit = 20;
		private const char historySeparator = (char) 10;
		// regular members
		public bool replaceMode;
		StringCollection findHistory = new StringCollection();
		StringCollection replaceHistory = new StringCollection();
		
		// services
		static PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));

		// gtk widgets
		[Glade.Widget] Gtk.Entry searchPatternEntry;
		[Glade.Widget] Gtk.Entry replacePatternEntry;
		[Glade.Widget] Gtk.Button findHelpButton;
		[Glade.Widget] Gtk.Button findButton;
		[Glade.Widget] Gtk.Button markAllButton;
		[Glade.Widget] Gtk.Button closeButton;
		[Glade.Widget] Gtk.Button replaceButton;
		[Glade.Widget] Gtk.Button replaceAllButton;
		[Glade.Widget] Gtk.Button replaceHelpButton;
		[Glade.Widget] Gtk.CheckButton ignoreCaseCheckBox;
		[Glade.Widget] Gtk.CheckButton searchWholeWordOnlyCheckBox;
		[Glade.Widget] Gtk.CheckButton useSpecialSearchStrategyCheckBox;
		[Glade.Widget] Gtk.ComboBox specialSearchStrategyComboBox;
		[Glade.Widget] Gtk.ComboBox searchLocationComboBox;
		[Glade.Widget] Gtk.Label label1;
		[Glade.Widget] Gtk.Label label2;		
		[Glade.Widget] Gtk.Label searchLocationLabel;
		[Glade.Widget] Gtk.Dialog FindDialogWidget;
		[Glade.Widget] Gtk.Dialog ReplaceDialogWidget;
		Gtk.Dialog ReplaceDialogPointer;
		
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

			searchPatternEntry.Completion = new EntryCompletion ();
			searchPatternEntry.Completion.Model = new ListStore (typeof (string));
			searchPatternEntry.Completion.TextColumn = 0;
			
			// set button sensitivity
			findHelpButton.Sensitive = false;
			
			// set replace dialog properties 
			if (replaceMode)
			{
				replacePatternEntry.Completion = new EntryCompletion ();
				replacePatternEntry.Completion.Model = new ListStore (typeof (string));
				replacePatternEntry.Completion.TextColumn = 0;

				ReplaceDialogPointer = this.ReplaceDialogWidget;
				// set the label properties
				replaceButton.UseUnderline = true;
				replaceAllButton.UseUnderline = true;
				
				// set te size groups to include the replace dialog
				labels.AddWidget(label2);
				combos.AddWidget(replacePatternEntry);
				helpButtons.AddWidget(replaceHelpButton);
				
				replaceHelpButton.Sensitive = false;
			}
			else
			{
				ReplaceDialogPointer = this.FindDialogWidget;
				markAllButton.UseUnderline = true;
			}
			ReplaceDialogPointer.TransientFor = (Gtk.Window)WorkbenchSingleton.Workbench;
		}
		
		public ReplaceDialog(bool replaceMode)
		{
			// some members needed to initialise this dialog based on replace mode
			this.replaceMode = replaceMode;
			string dialogName = (replaceMode) ? "ReplaceDialogWidget" : "FindDialogWidget";
			
			// we must do it from *here* otherwise, we get this assembly, not the caller
			Glade.XML glade = new XML (null, "Base.glade", dialogName, null);
			glade.Autoconnect (this);
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
			store.AppendValues (GettextCatalog.GetString ("Entire Project"));
			
			searchLocationComboBox.Model = store;
			searchLocationComboBox.PackStart (cr, true);
			searchLocationComboBox.AddAttribute (cr, "text", 0);
			
			index = 0;
			switch (SearchReplaceManager.SearchOptions.DocumentIteratorType) {
				case DocumentIteratorType.AllOpenFiles:
					index = 1;
					break;
				case DocumentIteratorType.WholeCombine:
					SearchReplaceManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.CurrentDocument;
					break;
			}
			searchLocationComboBox.Active = index;
			
			searchPatternEntry.Text = SearchReplaceManager.SearchOptions.SearchPattern;
			
			// insert event handlers
			findButton.Clicked  += new EventHandler(FindNextEvent);
			closeButton.Clicked += new EventHandler(CloseDialogEvent);
			ReplaceDialogPointer.Close += new EventHandler(CloseDialogEvent);
			ReplaceDialogPointer.DeleteEvent += new DeleteEventHandler (OnDeleted);
			
			if (replaceMode) {
				ReplaceDialogPointer.Title = GettextCatalog.GetString ("Replace");
				replaceButton.Clicked    += new EventHandler(ReplaceEvent);
				replaceAllButton.Clicked += new EventHandler(ReplaceAllEvent);
				replacePatternEntry.Text = SearchReplaceManager.SearchOptions.ReplacePattern;
			} else {
				ReplaceDialogPointer.Title = GettextCatalog.GetString ("Find");
				markAllButton.Clicked    += new EventHandler(MarkAllEvent);
			}
			searchPatternEntry.SelectRegion(0, searchPatternEntry.Text.Length);
			
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
			searchPatternEntry.Text  = pattern;
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
			SearchReplaceManager.SearchOptions.SearchPattern  = searchPatternEntry.Text;
			if (replaceMode) {
				SearchReplaceManager.SearchOptions.ReplacePattern = replacePatternEntry.Text;
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
				case 2:
					SearchReplaceManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.WholeCombine;
					break;
			}
		}
		
		void FindNextEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.Text.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.FindNext();
			
			AddSearchHistoryItem(findHistory, searchPatternEntry.Text);
		}
		
		void ReplaceEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.Text.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.Replace();
			
			AddSearchHistoryItem(replaceHistory, replacePatternEntry.Text);
		}
		
		void ReplaceAllEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.Text.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.ReplaceAll();
			
			AddSearchHistoryItem(replaceHistory, replacePatternEntry.Text);
		}
		
		void MarkAllEvent(object sender, EventArgs e)
		{
			if (searchPatternEntry.Text.Length == 0)
				return;
			
			SetupSearchReplaceManager();
			SearchReplaceManager.MarkAll();			
			
			AddSearchHistoryItem(findHistory, searchPatternEntry.Text);
		}
		
		void CloseDialogEvent(object sender, EventArgs e)
		{
			ReplaceDialogPointer.Hide();
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

			if (history == findHistory)
				searchPatternEntry.Completion.Model = store;
			else if( history == replaceHistory)
				replacePatternEntry.Completion.Model = store;
		}
		
		// loads the history arrays from the property service
		// NOTE: a newline character separates the search history strings
		private void LoadHistoryValues()
		{
			object stringArray;
			// set the history in properties
			stringArray = propertyService.GetProperty("MonoDevelop.FindReplaceDialogs.FindHistory");
		
			if (stringArray != null) {
				string[] items = stringArray.ToString ().Split (historySeparator);
				ListStore store = new ListStore (typeof (string));

				if(items != null) {
					findHistory.AddRange (items);
					foreach (string i in items) {
						store.AppendValues (i);
					}
				}

				searchPatternEntry.Completion.Model = store;
			}
			
			// now do the replace history
			stringArray = propertyService.GetProperty ("MonoDevelop.FindReplaceDialogs.ReplaceHistory");
			
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
					
					replacePatternEntry.Completion.Model = store;
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
			propertyService.SetProperty ("MonoDevelop.FindReplaceDialogs.FindHistory", string.Join(historySeparator.ToString(), stringArray));
			
			// now do the replace history
			if (replaceMode)	{
				stringArray = new string[replaceHistory.Count];
				replaceHistory.CopyTo (stringArray, 0);				
				propertyService.SetProperty ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", string.Join(historySeparator.ToString(), stringArray));
			}
		}
		
		#region code to pretend to be a dialog (cause we cant inherit Dialog and use glade)
		public void Present()
		{
			ReplaceDialogPointer.Present();
		}
		
		public void Destroy()
		{
			// save the search and replace history to properties
			OnClosed ();
			ReplaceDialogPointer.Destroy();
		}
		
		public void ShowAll()
		{
			ReplaceDialogPointer.ShowAll();
			searchPatternEntry.SelectRegion (0, searchPatternEntry.Text.Length);
		}
		#endregion
		
		public Gtk.Dialog DialogPointer
		{
			get {
				return ReplaceDialogPointer;
			}
		}
	}
}
