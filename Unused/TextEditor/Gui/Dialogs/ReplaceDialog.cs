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
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
//using MonoDevelop.XmlForms;
//using MonoDevelop.Gui.XmlForms;
using MonoDevelop.TextEditor;
using MonoDevelop.EditorBindings.Search;

using Gtk;
using Glade;

namespace MonoDevelop.Gui.Dialogs
{
	public class ReplaceDialog
	{
		private const int HISTORY_LIMIT = 20;
		private const char HISTORY_SEPARATOR_CHAR = (char) 10;
		// regular members
		public bool replaceMode;
		StringCollection findHistory = new StringCollection();
		StringCollection replaceHistory = new StringCollection();
		
		// services
		ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
		static PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		static FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
		StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService (typeof (StringParserService));
		
		// gtk widgets
		[Glade.Widget] Gtk.Combo searchPatternComboBox;
		[Glade.Widget] Gtk.Combo replacePatternComboBox;
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
		[Glade.Widget] Gtk.OptionMenu specialSearchStrategyComboBox;
		[Glade.Widget] Gtk.OptionMenu searchLocationComboBox;
		[Glade.Widget] Gtk.Label label1;
		[Glade.Widget] Gtk.Label label2;
		[Glade.Widget] Gtk.Label searchLocationLabel;
		[Glade.Widget] Gtk.Dialog FindDialogWidget;
		[Glade.Widget] Gtk.Dialog ReplaceDialogWidget;
		Gtk.Dialog ReplaceDialogPointer;
		
		void InitDialog ()
		{
			label1.Text = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.FindWhat}");
			searchLocationLabel.Text = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.SearchIn}");		
			//findButton.Label = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.FindNextButton}");			
			//closeButton.Label = stringParserService.Parse ("${res:Global.CloseButtonText}");
			findButton.UseUnderline = true;			
			closeButton.UseUnderline = true;			
			ignoreCaseCheckBox.Label = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.CaseSensitive}");
			searchWholeWordOnlyCheckBox.Label = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.WholeWord}");
			useSpecialSearchStrategyCheckBox.Label = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.UseMethodLabel}");			
			
			
			//set up the size groups
			SizeGroup labels = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup combos = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup options = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup helpButtons = new SizeGroup(SizeGroupMode.Horizontal);
			SizeGroup checkButtons = new SizeGroup(SizeGroupMode.Horizontal);
			labels.AddWidget(label1);			
			combos.AddWidget(searchPatternComboBox);
			helpButtons.AddWidget(findHelpButton);
			checkButtons.AddWidget(ignoreCaseCheckBox);
			checkButtons.AddWidget(searchWholeWordOnlyCheckBox);
			checkButtons.AddWidget(useSpecialSearchStrategyCheckBox);
			checkButtons.AddWidget(searchLocationLabel);
			options.AddWidget(specialSearchStrategyComboBox);
			options.AddWidget(searchLocationComboBox);
			
			// set button sensitivity
			findHelpButton.Sensitive = false;
			
			// set replace dialog properties 
			if(replaceMode)
			{
				ReplaceDialogPointer = this.ReplaceDialogWidget;
				// set the label properties
				label2.Text = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.ReplaceWith}");
				//replaceButton.Label = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.ReplaceButton}");
				replaceAllButton.Label = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.ReplaceAllButton}");
				replaceButton.UseUnderline = true;
				replaceAllButton.UseUnderline = true;
				
				// set te size groups to include the replace dialog
				labels.AddWidget(label2);
				combos.AddWidget(replacePatternComboBox);
				helpButtons.AddWidget(replaceHelpButton);
				
				replaceHelpButton.Sensitive = false;
			}
			else
			{
				ReplaceDialogPointer = this.FindDialogWidget;
				markAllButton.UseUnderline = true;
				markAllButton.Label = stringParserService.Parse ("${res:Dialog.NewProject.SearchReplace.MarkAllButton}");
			}
		}
		
		public ReplaceDialog(bool replaceMode)
		{
			// some members needed to initialise this dialog based on replace mode
			this.replaceMode = replaceMode;
			string dialogName = (replaceMode) ? "ReplaceDialogWidget" : "FindDialogWidget";
			
			// we must do it from *here* otherwise, we get this assembly, not the caller
			Glade.XML glade = new XML (null, "texteditoraddin.glade", dialogName, null);
			glade.Autoconnect (this);
			InitDialog ();
			/*if (replaceMode) {
				//this.SetupFromXml(Path.Combine(propertyService.DataDirectory, @"resources\dialogs\ReplaceDialog.xfrm"));
				//ControlDictionary["replaceHelpButton"].Enabled = false;
				InitDialogForReplace ();
			} else {
				InitDialogForFind ();
				//this.SetupFromXml(Path.Combine(propertyService.DataDirectory, @"resources\dialogs\FindDialog.xfrm"));
			}*/
			
			//AcceptButton = (Button)ControlDictionary["findButton"];
			//CancelButton = (Button)ControlDictionary["closeButton"];
			
			LoadHistoryValues();
			
			ignoreCaseCheckBox.Active = !SearchReplaceManager.SearchOptions.IgnoreCase;
			searchWholeWordOnlyCheckBox.Active = SearchReplaceManager.SearchOptions.SearchWholeWordOnly;
			
			useSpecialSearchStrategyCheckBox.Active  = SearchReplaceManager.SearchOptions.SearchStrategyType != SearchStrategyType.Normal;
			useSpecialSearchStrategyCheckBox.Toggled += new EventHandler(SpecialSearchStrategyCheckBoxChangedEvent);
			
			Gtk.MenuItem tmpItem = new Gtk.MenuItem ("Wildcards");
			Gtk.Menu stratMenu = new Gtk.Menu ();
			stratMenu.Append (tmpItem);
			tmpItem = new Gtk.MenuItem (resourceService.GetString("Dialog.NewProject.SearchReplace.SearchStrategy.RegexSearch"));
			stratMenu.Append (tmpItem);
			specialSearchStrategyComboBox.Menu = stratMenu;
		
			uint index = 0;
			switch (SearchReplaceManager.SearchOptions.SearchStrategyType) {
				case SearchStrategyType.Normal:
				case SearchStrategyType.Wildcard:
					break;
				case SearchStrategyType.RegEx:
					index = 1;
					break;
			}
			specialSearchStrategyComboBox.SetHistory (index);
			
			Gtk.Menu locMenu = new Gtk.Menu ();
			tmpItem = new Gtk.MenuItem (resourceService.GetString("Global.Location.currentfile"));
			locMenu.Append (tmpItem);
			tmpItem = new Gtk.MenuItem (resourceService.GetString("Global.Location.allopenfiles"));
			locMenu.Append (tmpItem);
			tmpItem = new Gtk.MenuItem (resourceService.GetString("Global.Location.wholeproject"));
			locMenu.Append (tmpItem);
			
			searchLocationComboBox.Menu = locMenu;	
			
			index = 0;
			switch (SearchReplaceManager.SearchOptions.DocumentIteratorType) {
				case DocumentIteratorType.AllOpenFiles:
					index = 1;
					break;
				case DocumentIteratorType.WholeCombine:
					SearchReplaceManager.SearchOptions.DocumentIteratorType = DocumentIteratorType.CurrentDocument;
					break;
			}
			searchLocationComboBox.SetHistory (index);
			
			searchPatternComboBox.Entry.Text  = SearchReplaceManager.SearchOptions.SearchPattern;
			
			// insert event handlers
			findButton.Clicked  += new EventHandler(FindNextEvent);
			closeButton.Clicked += new EventHandler(CloseDialogEvent);
			ReplaceDialogPointer.Close += new EventHandler(CloseDialogEvent);
			ReplaceDialogPointer.DeleteEvent += new GtkSharp.DeleteEventHandler (OnDeleted);
			
			if (replaceMode) {
				ReplaceDialogPointer.Title = resourceService.GetString("Dialog.NewProject.SearchReplace.ReplaceDialogName");
				replaceButton.Clicked    += new EventHandler(ReplaceEvent);
				replaceAllButton.Clicked += new EventHandler(ReplaceAllEvent);
				replacePatternComboBox.Entry.Text = SearchReplaceManager.SearchOptions.ReplacePattern;
			} else {
				ReplaceDialogPointer.Title = resourceService.GetString("Dialog.NewProject.SearchReplace.FindDialogName");
				markAllButton.Clicked    += new EventHandler(MarkAllEvent);
			}
			
				//ControlDictionary["replacePatternComboBox"].Visible = false;
				//ControlDictionary["replaceAllButton"].Visible       = false;
				//ControlDictionary["replacePatternLabel"].Visible    = false;
				//ControlDictionary["replacePatternButton"].Visible   = false;
				//ControlDictionary["replaceButton"].Text             = resourceService.GetString("Dialog.NewProject.SearchReplace.ToggleReplaceModeButton");
				//ClientSize = new Size(ClientSize.Width, ClientSize.Height - 32);
			
			SpecialSearchStrategyCheckBoxChangedEvent(null, null);
			SearchReplaceManager.ReplaceDialog     = this;
		}
		
		protected void OnClosed()
		{
			SaveHistoryValues();
			
		}
		
		void OnDeleted (object o, GtkSharp.DeleteEventArgs args)
		{
			// perform the standard closing windows event
			OnClosed();
			SearchReplaceManager.ReplaceDialog = null;
		}

		public void SetSearchPattern(string pattern)
		{
			searchPatternComboBox.Entry.Text  = pattern;
		}
		
		void SetupSearchReplaceManager()
		{
			SearchReplaceManager.SearchOptions.SearchPattern  = searchPatternComboBox.Entry.Text;
			if (replaceMode) {
				SearchReplaceManager.SearchOptions.ReplacePattern = replacePatternComboBox.Entry.Text;
			}
			
			SearchReplaceManager.SearchOptions.IgnoreCase          = !ignoreCaseCheckBox.Active;
			SearchReplaceManager.SearchOptions.SearchWholeWordOnly = searchWholeWordOnlyCheckBox.Active;
			
			if (useSpecialSearchStrategyCheckBox.Active) {
				switch (specialSearchStrategyComboBox.History) {
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
			
			switch (searchLocationComboBox.History) {
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
			if (searchPatternComboBox.Entry.Text.Length == 0) {
				return;
			}
			
			try {
				//Cursor = Cursors.WaitCursor;
				SetupSearchReplaceManager();
				SearchReplaceManager.FindNext();
				//this.Focus();
			}
			finally {
				//Cursor = Cursors.Default;
			}
			
			AddSearchHistoryItem(findHistory, searchPatternComboBox.Entry.Text);
		}
		
		void ReplaceEvent(object sender, EventArgs e)
		{
			if (searchPatternComboBox.Entry.Text.Length == 0) {
				return;
			}
			
			try {
				//Cursor = Cursors.WaitCursor;
				
				SetupSearchReplaceManager();
				SearchReplaceManager.Replace();
			}
			finally {
				//Cursor = Cursors.Default;
			}
			
			AddSearchHistoryItem(replaceHistory, replacePatternComboBox.Entry.Text);
		}
		
		void ReplaceAllEvent(object sender, EventArgs e)
		{
			if (searchPatternComboBox.Entry.Text.Length == 0) {
				return;
			}
			
			try {
				//Cursor = Cursors.WaitCursor;
				
				SetupSearchReplaceManager();
				SearchReplaceManager.ReplaceAll();
			} finally {
				//Cursor = Cursors.Default;
			}
			
			AddSearchHistoryItem(replaceHistory, replacePatternComboBox.Entry.Text);
		}
		
		void MarkAllEvent(object sender, EventArgs e)
		{
			if (searchPatternComboBox.Entry.Text.Length == 0) {
				return;
			}
			
			try {
				//Cursor = Cursors.WaitCursor;
				
				SetupSearchReplaceManager();
				SearchReplaceManager.MarkAll();			
			} finally {
				//Cursor = Cursors.Default;
			}
			
			AddSearchHistoryItem(findHistory, searchPatternComboBox.Entry.Text);
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
			}
		}
		
		// generic method to add a string to a history item
		private void AddSearchHistoryItem(StringCollection history, string toAdd)
		{
			// add the item to the find history
			if (history.Contains(toAdd)) {
				// remove it so it gets added at the top
				history.Remove(toAdd);
			}
			// make sure there is only 20
			if (history.Count == HISTORY_LIMIT) {
				history.RemoveAt(HISTORY_LIMIT - 1);
			}
			history.Insert(0, toAdd);
			
			// update the drop down for the combobox
			string[] stringArray = new string[history.Count];
			history.CopyTo(stringArray, 0);
			if (history == findHistory) {
				searchPatternComboBox.SetPopdownStrings(stringArray);
			} else if( history == replaceHistory) {
				replacePatternComboBox.SetPopdownStrings(stringArray);
			}
		}
		
		// loads the history arrays from the property service
		// NOTE: this dialog uses a newline character to separate search history strings in the properties file 
		private void LoadHistoryValues()
		{
			object stringArray;
			// set the history in properties
			stringArray = propertyService.GetProperty("MonoDevelop.FindReplaceDialogs.FindHistory");
		
			if(stringArray != null) {
				findHistory.AddRange(stringArray.ToString().Split(HISTORY_SEPARATOR_CHAR));
				searchPatternComboBox.SetPopdownStrings(stringArray.ToString().Split(HISTORY_SEPARATOR_CHAR));
			}
			
			// now do the replace history
			if(replaceMode)	{					
				stringArray = propertyService.GetProperty("MonoDevelop.FindReplaceDialogs.ReplaceHistory");
				if(stringArray != null) {
					replaceHistory.AddRange(stringArray.ToString().Split(HISTORY_SEPARATOR_CHAR));
					replacePatternComboBox.SetPopdownStrings(stringArray.ToString().Split(HISTORY_SEPARATOR_CHAR));
				}
			}
		}
		
		// saves the history arrays to the property service
		// NOTE: this dialog uses a newline character to separate search history strings in the properties file
		private void SaveHistoryValues()
		{
			string[] stringArray;
			// set the history in properties
			stringArray = new string[findHistory.Count];
			findHistory.CopyTo(stringArray, 0);			
			propertyService.SetProperty("MonoDevelop.FindReplaceDialogs.FindHistory", string.Join(HISTORY_SEPARATOR_CHAR.ToString(), stringArray));
			
			// now do the replace history
			if(replaceMode)	{
				stringArray = new string[replaceHistory.Count];
				replaceHistory.CopyTo(stringArray, 0);				
				propertyService.SetProperty("MonoDevelop.FindReplaceDialogs.ReplaceHistory", string.Join(HISTORY_SEPARATOR_CHAR.ToString(), stringArray));
			}
		}
		
		#region code to pretend to be a dialog (cause we can't inherit Dialog and use glade)
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
		}
		#endregion
	}
}
