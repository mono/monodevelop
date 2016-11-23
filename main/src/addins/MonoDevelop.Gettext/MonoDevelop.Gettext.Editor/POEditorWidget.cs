//
// POEditorWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Gettext.Editor;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using System.ComponentModel;
using System.Threading;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Gettext
{
	partial class POEditorWidget : Gtk.Bin, IUndoHandler
	{
		TranslationProject project;
		CatalogHeadersWidget headersEditor;
		ListStore store;
		ListStore foundInStore;
		Catalog catalog;
		string  poFileName;
		TextEditor texteditorOriginal = TextEditorFactory.CreateNewEditor ();
		TextEditor texteditorPlural = TextEditorFactory.CreateNewEditor ();
		
		static List<POEditorWidget> widgets = new List<POEditorWidget> (); 
		
		//simple escaping to make the messages display in single lines in the treeview
		static string EscapeForTreeView (string message)
		{
			return StringEscaping.ToGettextFormat (message);
		}
		
		public Catalog Catalog {
			get {
				return catalog;
			}
			set {
				catalog = value;
				headersEditor.CatalogHeaders = catalog;
				ClearTextview ();
				AddTextview (0);
				UpdateFromCatalog ();
				UpdateProgressBar ();
			}
		}
		
		public string POFileName { // todo - move to Catalog class.
			get {
				return poFileName;
			}
			set {
				poFileName = value;
			}
		}
		
		internal static readonly Gdk.Color errorColor = new Gdk.Color (210, 32, 32);
		public POEditorWidget (TranslationProject project)
		{
			this.project = project;
			this.Build ();
			
			//FIXME: avoid unnecessary creation of old treeview
			scrolledwindow1.Remove (treeviewEntries);
			treeviewEntries.Destroy ();
			treeviewEntries = new MonoDevelop.Components.ContextMenuTreeView ();
			treeviewEntries.ShowAll ();
			scrolledwindow1.Add (treeviewEntries);
			((MonoDevelop.Components.ContextMenuTreeView)treeviewEntries).DoPopupMenu = ShowPopup;
			
			this.headersEditor = new CatalogHeadersWidget ();
			this.notebookPages.AppendPage (headersEditor, new Gtk.Label ());
			
			updateTaskThread = new BackgroundWorker ();
			updateTaskThread.WorkerSupportsCancellation = true;
			updateTaskThread.DoWork += TaskUpdateWorker;
			
			AddButton (GettextCatalog.GetString ("Translation")).Active = true;
			AddButton (GettextCatalog.GetString ("Headers")).Active = false;
			
			// entries tree view 
			store = new ListStore (typeof(CatalogEntry));
			this.treeviewEntries.Model = store;
			
			TreeViewColumn fuzzyColumn = new TreeViewColumn ();
			fuzzyColumn.SortIndicator = true;
			fuzzyColumn.SortColumnId = 0;
				
			fuzzyColumn.Title = GettextCatalog.GetString ("Fuzzy");
			var iconRenderer = new CellRendererImage ();
			fuzzyColumn.PackStart (iconRenderer, false);
			fuzzyColumn.SetCellDataFunc (iconRenderer, CatalogIconDataFunc);
			
			CellRendererToggle cellRendFuzzy = new CellRendererToggle ();
			cellRendFuzzy.Activatable = true;
			cellRendFuzzy.Toggled += HandleCellRendFuzzyToggled;
			fuzzyColumn.PackStart (cellRendFuzzy, false);
			fuzzyColumn.SetCellDataFunc (cellRendFuzzy, FuzzyToggleDataFunc);
			treeviewEntries.AppendColumn (fuzzyColumn);
			
			TreeViewColumn originalColumn = new TreeViewColumn ();
			originalColumn.Expand = true;
			originalColumn.SortIndicator = true;
			originalColumn.SortColumnId = 1;
			originalColumn.Title = GettextCatalog.GetString ("Original string");
			CellRendererText original = new CellRendererText ();
			original.Ellipsize = Pango.EllipsizeMode.End;
			originalColumn.PackStart (original, true);
			originalColumn.SetCellDataFunc (original, OriginalTextDataFunc);
			treeviewEntries.AppendColumn (originalColumn);
			
			TreeViewColumn translatedColumn = new TreeViewColumn ();
			translatedColumn.Expand = true;
			translatedColumn.SortIndicator = true;
			translatedColumn.SortColumnId = 2;
			translatedColumn.Title = GettextCatalog.GetString ("Translated string");
			CellRendererText translation = new CellRendererText ();
			translation.Ellipsize = Pango.EllipsizeMode.End;
			translatedColumn.PackStart (translation, true);
			translatedColumn.SetCellDataFunc (translation, TranslationTextDataFunc);
			treeviewEntries.AppendColumn (translatedColumn);
			
			treeviewEntries.Selection.Changed += OnEntrySelected;
			
			// found in tree view
			foundInStore = new ListStore (typeof(string), typeof(string), typeof(string), typeof(Xwt.Drawing.Image));
			this.treeviewFoundIn.Model = foundInStore;
			
			TreeViewColumn fileColumn = new TreeViewColumn ();
			var pixbufRenderer = new CellRendererImage ();
			fileColumn.PackStart (pixbufRenderer, false);
			fileColumn.SetAttributes (pixbufRenderer, "image", FoundInColumns.Pixbuf);
			
			CellRendererText textRenderer = new CellRendererText ();
			fileColumn.PackStart (textRenderer, true);
			fileColumn.SetAttributes (textRenderer, "text", FoundInColumns.File);
			treeviewFoundIn.AppendColumn (fileColumn);
			
			treeviewFoundIn.AppendColumn ("", new CellRendererText (), "text", FoundInColumns.Line);
			treeviewFoundIn.HeadersVisible = false;
			treeviewFoundIn.GetColumn (1).FixedWidth = 100;
			
			treeviewFoundIn.RowActivated += delegate(object sender, RowActivatedArgs e) {
				Gtk.TreeIter iter;
				foundInStore.GetIter (out iter, e.Path);
				string line = foundInStore.GetValue (iter, (int)FoundInColumns.Line) as string;
				string file = foundInStore.GetValue (iter, (int)FoundInColumns.FullFileName) as string;
				int lineNr = 1;
				try {
					lineNr = Math.Max(1, int.Parse (line));
				} catch {
				}
				IdeApp.Workbench.OpenDocument (new FileOpenInformation (file, project, lineNr, 1, OpenDocumentOptions.Default));
			};
			this.notebookTranslated.RemovePage (0);
			this.searchEntryFilter.Entry.Text = "";
			searchEntryFilter.Entry.Changed += delegate {
				UpdateFromCatalog ();
			};
			
			this.togglebuttonFuzzy.Active = PropertyService.Get ("Gettext.ShowFuzzy", true);
			this.togglebuttonFuzzy.TooltipText = GettextCatalog.GetString ("Show fuzzy translations");
			this.togglebuttonFuzzy.Toggled += delegate {
				MonoDevelop.Core.PropertyService.Set ("Gettext.ShowFuzzy", this.togglebuttonFuzzy.Active);
				UpdateFromCatalog ();
			};
			
			this.togglebuttonMissing.Active = PropertyService.Get ("Gettext.ShowMissing", true);
			this.togglebuttonMissing.TooltipText = GettextCatalog.GetString ("Show missing translations");
			this.togglebuttonMissing.Toggled += delegate {
				MonoDevelop.Core.PropertyService.Set ("Gettext.ShowMissing", this.togglebuttonMissing.Active);
				UpdateFromCatalog ();
			};
			
			this.togglebuttonOk.Active = PropertyService.Get ("Gettext.ShowTranslated", true);
			this.togglebuttonOk.TooltipText = GettextCatalog.GetString ("Show valid translations");
			this.togglebuttonOk.Toggled += delegate {
				MonoDevelop.Core.PropertyService.Set ("Gettext.ShowTranslated", this.togglebuttonOk.Active);
				UpdateFromCatalog ();
			};
			
			this.textviewComments.Buffer.Changed += delegate {
				if (this.isUpdating)
					return;
				if (this.currentEntry != null) {
					string[] lines =  textviewComments.Buffer.Text.Split (CatalogParser.LineSplitStrings, System.StringSplitOptions.None);
					for (int i = 0; i < lines.Length; i++) {
						if (!lines[i].StartsWith ("#"))
							lines[i] = "# " + lines[i];
					}
					this.currentEntry.Comment = string.Join (System.Environment.NewLine, lines);
				}
				UpdateProgressBar ();
			};
			
			searchEntryFilter.Ready = true;
			searchEntryFilter.Visible = true;
			searchEntryFilter.ForceFilterButtonVisible = true;
			searchEntryFilter.RequestMenu += delegate {
				searchEntryFilter.Menu = CreateOptionsMenu ();
			};
			
			widgets.Add (this);
			
			checkbuttonWhiteSpaces.Toggled += CheckbuttonWhiteSpacesToggled;

			this.scrolledwindowOriginal.Child = this.texteditorOriginal;
			this.scrolledwindowPlural.Child = this.texteditorPlural;
			this.scrolledwindowOriginal.Child.Show ();
			this.scrolledwindowPlural.Child.Show ();
			scrolledwindowOriginal.Child.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Insensitive));
			scrolledwindowPlural.Child.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Insensitive));
			this.texteditorOriginal.Options = DefaultSourceEditorOptions.PlainEditor;
			this.texteditorPlural.Options = DefaultSourceEditorOptions.PlainEditor;
			this.texteditorOriginal.IsReadOnly = true;
			this.texteditorPlural.IsReadOnly = true;
			toolbarPages.ModifyBg (StateType.Normal, Styles.POEditor.TabBarBackgroundColor);

			MonoDevelop.Ide.Gui.Styles.Changed += HandleStylesChanged;
		}

		void HandleStylesChanged (object sender, EventArgs e)
		{
			UpdateFromCatalog ();
			toolbarPages.ModifyBg (StateType.Normal, Styles.POEditor.TabBarBackgroundColor);
		}

		void HandleCellRendFuzzyToggled (object sender, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				CatalogEntry entry = (CatalogEntry)store.GetValue (iter, 0);
				entry.IsFuzzy = !entry.IsFuzzy; 
				UpdateProgressBar ();
			}
		}

		void CatalogIconDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CatalogEntry entry = (CatalogEntry)model.GetValue (iter, 0);
			((CellRendererImage)cell).Image = ImageService.GetIcon (GetStockForEntry (entry), IconSize.Menu);
			cell.CellBackgroundGdk = GetRowColorForEntry (entry);
		}
		
		void FuzzyToggleDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CatalogEntry entry = (CatalogEntry)model.GetValue (iter, 0);
			((CellRendererToggle)cell).Active = entry.IsFuzzy;
			cell.CellBackgroundGdk = GetRowColorForEntry (entry);
		}
		
		void OriginalTextDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CatalogEntry entry = (CatalogEntry)model.GetValue (iter, 0);
			((CellRendererText)cell).Text = EscapeForTreeView (entry.String);
			cell.CellBackgroundGdk = GetRowColorForEntry (entry);
			((CellRendererText)cell).ForegroundGdk = GetForeColorForEntry (entry);
		}
		
		void TranslationTextDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CatalogEntry entry = (CatalogEntry)model.GetValue (iter, 0);
			((CellRendererText)cell).Text = EscapeForTreeView (entry.GetTranslation (0));
			cell.CellBackgroundGdk = GetRowColorForEntry (entry);
			((CellRendererText)cell).ForegroundGdk = GetForeColorForEntry (entry);
		}
		
		void CheckbuttonWhiteSpacesToggled (object sender, EventArgs e)
		{
		}
		
		#region Options
		enum SearchIn {
			Original,
			Translated,
			Both
		}
		
		static bool isCaseSensitive;
		static bool isWholeWordOnly;
		static bool regexSearch;
		static SearchIn searchIn;
		
		static POEditorWidget ()
		{
			isCaseSensitive = PropertyService.Get ("GettetAddin.Search.IsCaseSensitive", false);
			isWholeWordOnly = PropertyService.Get ("GettetAddin.Search.IsWholeWordOnly", false);
			regexSearch     = PropertyService.Get ("GettetAddin.Search.RegexSearch", false);
			searchIn        = PropertyService.Get ("GettetAddin.Search.SearchIn", SearchIn.Both);
		}
		
		static bool IsCaseSensitive {
			get {
				return isCaseSensitive;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.IsCaseSensitive", value);
				isCaseSensitive = value;
			}
		}
		
		static bool IsWholeWordOnly {
			get {
				return isWholeWordOnly;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.IsWholeWordOnly", value);
				isWholeWordOnly = value;
			}
		}
		
		static bool RegexSearch {
			get {
				return regexSearch;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.RegexSearch", value);
				regexSearch = value;
			}
		}
		
		static SearchIn DoSearchIn {
			get {
				return searchIn;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.SearchIn", value);
				searchIn = value;
			}
		}
		#endregion
		
		public Menu CreateOptionsMenu ()
		{
			Menu menu = new Menu ();
			
			MenuItem searchInMenu = new MenuItem (GettextCatalog.GetString ("_Search in"));
			Menu sub = new Menu ();
			searchInMenu.Submenu = sub;
			Gtk.RadioMenuItem  original = null, translated = null, both = null;
			GLib.SList group = new GLib.SList (IntPtr.Zero);
			original = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Original"));
			group = original.Group;
			original.ButtonPressEvent += delegate { original.Activate (); };
			sub.Append (original);
			
			translated = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Translated"));
			translated.ButtonPressEvent += delegate { translated.Activate (); };
			group = translated.Group;
			sub.Append (translated);
			
			both = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Both"));
			both.ButtonPressEvent += delegate { both.Activate (); };
			sub.Append (both);
			switch (DoSearchIn) {
			case SearchIn.Both:
				both.Activate ();
				break;
			case SearchIn.Original:
				original.Activate ();
				break;
			case SearchIn.Translated:
				translated.Activate ();
				break;
			}
			menu.Append (searchInMenu);
			both.Activated += delegate {
				if (DoSearchIn != SearchIn.Both) {
					DoSearchIn = SearchIn.Both;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			original.Activated += delegate {
				if (DoSearchIn != SearchIn.Original) {
					DoSearchIn = SearchIn.Original;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			translated.Activated += delegate {
				if (DoSearchIn != SearchIn.Translated) {
					DoSearchIn = SearchIn.Translated;
					UpdateFromCatalog ();
					menu.Destroy ();
				}
			};
			
			Gtk.CheckMenuItem regexSearch = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
			regexSearch.Active = RegexSearch;
			regexSearch.ButtonPressEvent += delegate { 
				RegexSearch = !RegexSearch;
				UpdateFromCatalog ();
			};
			menu.Append (regexSearch);
			
			Gtk.CheckMenuItem caseSensitive = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
			caseSensitive.Active = IsCaseSensitive;
			caseSensitive.ButtonPressEvent += delegate { 
				IsCaseSensitive = !IsCaseSensitive;
				UpdateFromCatalog ();
			};
			menu.Append (caseSensitive);
			
			Gtk.CheckMenuItem wholeWordsOnly = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
			wholeWordsOnly.Active = IsWholeWordOnly;
			wholeWordsOnly.Sensitive = !RegexSearch;
			wholeWordsOnly.ButtonPressEvent += delegate {
				IsWholeWordOnly = !IsWholeWordOnly;
				UpdateFromCatalog ();
			};
			menu.Append (wholeWordsOnly);
			menu.ShowAll ();
			return menu;
		}
		
		public static void ReloadWidgets ()
		{
			foreach (POEditorWidget widget in widgets) {
				widget.Reload ();
			}
		}
		
		void Reload ()
		{
			Catalog newCatalog = new Catalog(project);
			newCatalog.Load (null, catalog.FileName);
			this.Catalog = newCatalog;
			UpdateTasks ();
		}
		List<TextEditor> notebookTranslatedEditors = new List<TextEditor> ();
		TextEditor GetTextView (int index)
		{
			return notebookTranslatedEditors[index];
		}
		
		void ClearTextview ()
		{
			while (this.notebookTranslated.NPages > 0)
				this.notebookTranslated.RemovePage (0);
		}
		
		void AddTextview (int index)
		{
			ScrolledWindow window = new ScrolledWindow ();
			var textView = TextEditorFactory.CreateNewEditor ();
			window.Child = textView;
			textView.TextChanged += delegate {
				if (this.isUpdating)
					return;
				try {
					if (this.currentEntry != null) {
						string escapedText = textView.Text;
						string oldText     = this.currentEntry.GetTranslation (index);
						this.currentEntry.SetTranslation (escapedText, index);
						AddChange (this.currentEntry, oldText, escapedText, index);
					}
					IdeApp.Workbench.StatusBar.ShowReady ();
					window.Child.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
				} catch (System.Exception e) {
					IdeApp.Workbench.StatusBar.ShowError (e.Message);
					window.Child.ModifyBase (Gtk.StateType.Normal, errorColor);
				}
				treeviewEntries.QueueDraw ();
				UpdateProgressBar ();
				UpdateTasks ();
			};
			
			Label label = new Label ();
			label.Text = this.Catalog.PluralFormsDescriptions [index];
			window.ShowAll ();
			this.notebookTranslated.AppendPage (window, label);
			notebookTranslatedEditors.Add (textView);
		}
		
		void ShowPopup (EventButton evt)
		{
			Gtk.Menu contextMenu = CreateContextMenu ();
			if (contextMenu != null)
				GtkWorkarounds.ShowContextMenu (contextMenu, this, evt);
		}
		
		Gtk.Menu CreateContextMenu ()
		{
			CatalogEntry entry = SelectedEntry;
			if (entry == null)
				return null;

			Gtk.Menu result = new Gtk.Menu ();
			
			Gtk.MenuItem item = new Gtk.MenuItem ("Delete");
			item.Sensitive = entry.References.Length == 0;
			item.Activated += delegate {
				RemoveEntry (entry);
			};
			item.Show();
			result.Append (item);
			
			return result;
		}
		
		void RemoveEntryByString (string msgstr)
		{
			CatalogEntry entry = this.catalog.FindItem (msgstr);
			if (entry != null) { 
				if (currentEntry.String == msgstr) 
					this.EditEntry (null);
				this.catalog.RemoveItem (entry);
				this.UpdateFromCatalog ();
			}
		}
		
		void RemoveEntry (CatalogEntry entry)
		{
			bool yes = MessageService.AskQuestion (GettextCatalog.GetString ("Do you really want to remove the translation string {0} (It will be removed from all translations)?", entry.String),
			                                                            AlertButton.Cancel, AlertButton.Remove) == AlertButton.Remove;

			if (yes) {
				TranslationProject project = IdeApp.ProjectOperations.CurrentSelectedSolutionItem as TranslationProject;
				if (project != null) {
					foreach (POEditorWidget widget in widgets)
						widget.RemoveEntryByString (entry.String);
					project.RemoveEntry (entry.String);
				}
			}
		}
		
		void UpdateProgressBar ()
		{
			int all, untrans, fuzzy, missing, bad;
			catalog.GetStatistics (out all, out fuzzy, out missing, out bad, out untrans);
			double percentage = all > 0 ? ((double)(all - untrans) / all) * 100 : 0.0;
			string barText = String.Format (GettextCatalog.GetString ("{0:#00.00}% Translated"), percentage);
			if (untrans > 0 || fuzzy > 0)
				barText += " (";

			if (untrans > 0) {
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Missing Message", "{0} Missing Messages", untrans), untrans);
			}

			if (fuzzy > 0) {
				if (untrans > 0) {
					barText += ", ";
				}
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Fuzzy Message", "{0} Fuzzy Messages", fuzzy), fuzzy);
			}

			if (untrans > 0 || fuzzy > 0)
				barText += ")";
			
			this.progressbar1.Text = barText;
			percentage = percentage / 100;
			this.progressbar1.Fraction = percentage;
		}
		
		#region EntryEditor handling
		CatalogEntry currentEntry;
//		Dictionary<Mono.TextEditor.TextEditor, bool> gtkSpellSet = new Dictionary<Mono.TextEditor.TextEditor, bool> (); 
		void RemoveTextViewsFrom (int index)
		{
			for (int i = this.notebookTranslated.NPages - 1; i >= index; i--) {
				var view = GetTextView (i);
				if (view == null)
					continue;
//				if (gtkSpellSet.ContainsKey (view)) {
//					GtkSpell.Detach (view);
//					gtkSpellSet.Remove (view);
//				}
				notebookTranslated.RemovePage (i);
				notebookTranslatedEditors.RemoveAt (i); 
			}
		}
		
		void EditEntry (CatalogEntry entry)
		{
			this.isUpdating = true;
			try {
				currentEntry = entry;
				this.texteditorOriginal.CaretLocation = new DocumentLocation (1, 1);
				this.texteditorOriginal.Text = entry != null ? entry.String : "";
				//this.texteditorOriginal.VAdjustment.Value = this.texteditorOriginal.HAdjustment.Value = 0;
				
//				if (GtkSpell.IsSupported && !gtkSpellSet.ContainsKey (this.textviewOriginal)) {
//					GtkSpell.Attach (this.textviewOriginal, "en");
//					this.gtkSpellSet[this.textviewOriginal] = true;
//				}
//				
				this.vbox8.Visible = entry != null && entry.HasPlural;
				this.notebookTranslated.ShowTabs = entry != null && entry.HasPlural;
				
				if (entry != null && entry.HasPlural) {
					this.texteditorPlural.CaretLocation = new DocumentLocation (1, 1);
					this.texteditorPlural.Text = entry.PluralString;
					//this.texteditorPlural.VAdjustment.Value = this.texteditorPlural.HAdjustment.Value = 0;
//					if (GtkSpell.IsSupported && !gtkSpellSet.ContainsKey (this.textviewOriginalPlural)) {
//						GtkSpell.Attach (this.textviewOriginalPlural, "en");
//						this.gtkSpellSet[this.textviewOriginalPlural] = true;
//					}
				}
				
				this.foundInStore.Clear ();
				
				if (entry != null) { 
					RemoveTextViewsFrom (entry.NumberOfTranslations);
					
					for (int i = this.notebookTranslated.NPages; i < entry.NumberOfTranslations; i++) {
						AddTextview (i);
					}
					
					for (int i = 0; i < entry.NumberOfTranslations; i++) {
						var textView = GetTextView (i);
						if (textView == null)
							continue;
						textView.ClearSelection ();
						textView.Text = entry != null ?  entry.GetTranslation (i) : "";
						EditActions.MoveCaretToDocumentEnd (textView);
					}
					
					foreach (string reference in entry.References) {
						string file;
						string line;
						int i = reference.IndexOf (':');
						if (i >= 0) {
							file = reference.Substring (0, i);
							line = reference.Substring (i + 1);
						} else {
							file = reference;
							line = "?";
						}
						string fullName = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (this.poFileName), file);
						this.foundInStore.AppendValues (file, line, fullName, DesktopService.GetIconForFile (fullName, IconSize.Menu));
					}
				}
				
				this.textviewComments.Buffer.Text = entry != null ?  entry.Comment : null;
				
/*				if (GtkSpell.IsSupported) {
					foreach (TextView view in this.gtkSpellSet.Keys)
						GtkSpell.Recheck (view);
				}*/
			} finally {
				this.isUpdating = false;
			}
		}
		#endregion
		
#region TreeView handling
		enum FoundInColumns : int
		{
			File,
			Line,
			FullFileName,
			Pixbuf
		}
		
		static string GetStockForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return Gtk.Stock.DialogError;
			return entry.IsFuzzy ? iconFuzzy : entry.IsTranslated ? iconValid : iconMissing;
		}
		
		static string iconFuzzy   = "md-error";// "md-translation-fuzzy";
		static string iconValid   = "md-done";//"md-translation-valid";
		static string iconMissing = "md-warning";//"md-translation-missing";
		
		Color GetRowColorForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return Styles.POEditor.EntryMissingBackgroundColor;
			return entry.IsFuzzy ? Styles.POEditor.EntryFuzzyBackgroundColor : entry.IsTranslated ? Style.Base (StateType.Normal) : Styles.POEditor.EntryUntranslatedBackgroundColor;
		}
		
		Color GetForeColorForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return Styles.POEditor.EntryMissingBackgroundColor;
			return entry.IsFuzzy ? Style.Black : entry.IsTranslated ? Style.Text (StateType.Normal) : Style.Black;
		}
		
		static int GetTypeSortIndicator (CatalogEntry entry)
		{
			return entry.IsFuzzy ? 1 : entry.IsTranslated ? 0 : 2;
		}
			
		TreeIter SelectedIter {
			get {
				TreeIter iter;
				if (treeviewEntries.Selection.GetSelected (out iter)) 
					return iter;
				return Gtk.TreeIter.Zero;
			}
		}
			
		CatalogEntry SelectedEntry {
			get {
				TreeIter iter = SelectedIter;
				if (iter.Equals (Gtk.TreeIter.Zero))
					return null;
				if (treeviewEntries.Selection.IterIsSelected (iter))
					return store.GetValue (iter, 0) as CatalogEntry;
				return null;
			}
		}
		
		void OnEntrySelected (object sender, EventArgs args)
		{			
			CatalogEntry entry = SelectedEntry;
			if (entry != null)
				EditEntry (entry);
		}

		bool IsMatch (string text, string filter)
		{
			if (RegexSearch)
				return regex.IsMatch (text);
		
			if (!IsCaseSensitive)
				text = text.ToUpper ();
			int idx = text.IndexOf (filter);
			if (idx >= 0) {
				if (IsWholeWordOnly) {
					return (idx == 0 || char.IsWhiteSpace (text[idx - 1])) &&
						   (idx + filter.Length == text.Length || char.IsWhiteSpace (text[idx + 1]));
				}
				return true;
			}
			return false;
		}
		
		bool ShouldFilter (CatalogEntry entry, string filter)
		{
			if (entry.IsFuzzy) {
				if (!this.togglebuttonFuzzy.Active) {
					return true;
				}
			} else {
				if (!entry.IsTranslated && !this.togglebuttonMissing.Active)
					return true;
				if (entry.IsTranslated && !this.togglebuttonOk.Active)
					return true;
			}
			
			if (String.IsNullOrEmpty (filter)) 
				return false;
			if (DoSearchIn != SearchIn.Translated) {
				if (IsMatch (entry.String, filter))
					return false;
				if (entry.HasPlural) {
					if (IsMatch (entry.PluralString, filter))
						return false;
				}
			}
			
			if (DoSearchIn != SearchIn.Original) {
				for (int i = 0; i < entry.NumberOfTranslations; i++) {
					if (IsMatch (entry.GetTranslation (i), filter))
						return false;
				}
			}
			return true;
		}
		
		string filter = "";
		System.Text.RegularExpressions.Regex  regex = new System.Text.RegularExpressions.Regex ("");
		
		void UpdateFromCatalog ()
		{
			filter = this.searchEntryFilter.Entry.Text;
			if (!IsCaseSensitive && filter != null)
				filter = filter.ToUpper ();
			if (RegexSearch) {
				try {
					RegexOptions options = RegexOptions.Compiled;
					if (!IsCaseSensitive)
						options |= RegexOptions.IgnoreCase;
					regex = new System.Text.RegularExpressions.Regex (filter, options);
				} catch (Exception e) {
					IdeApp.Workbench.StatusBar.ShowError (e.Message);
					this.searchEntryFilter.Entry.ModifyBase (StateType.Normal, errorColor);
					this.searchEntryFilter.QueueDraw ();
					return;
				}
			}
			this.searchEntryFilter.Entry.ModifyBase (StateType.Normal, Style.Base (StateType.Normal));
			this.searchEntryFilter.QueueDraw ();
			
			int found = 0;
			ListStore newStore = new ListStore (typeof(CatalogEntry));
			
			try {
				foreach (CatalogEntry entry in catalog) {
					if (!ShouldFilter (entry, filter)) {
						newStore.AppendValues (entry);
						found++;
					}
				}
			} catch (Exception) {
				
			}
			
			newStore.SetSortFunc (0, delegate (TreeModel model, TreeIter iter1, TreeIter iter2) {
				CatalogEntry entry1 = (CatalogEntry)model.GetValue (iter1, 0);
				CatalogEntry entry2 = (CatalogEntry)model.GetValue (iter2, 0);
				return GetTypeSortIndicator (entry1).CompareTo (GetTypeSortIndicator (entry2));
			});
			newStore.SetSortFunc (1, delegate (TreeModel model, TreeIter iter1, TreeIter iter2) {
				CatalogEntry entry1 = (CatalogEntry)model.GetValue (iter1, 0);
				CatalogEntry entry2 = (CatalogEntry)model.GetValue (iter2, 0);
				return entry1.String.CompareTo (entry2.String);
			});
			newStore.SetSortFunc (2, delegate (TreeModel model, TreeIter iter1, TreeIter iter2) {
				CatalogEntry entry1 = (CatalogEntry)model.GetValue (iter1, 0);
				CatalogEntry entry2 = (CatalogEntry)model.GetValue (iter2, 0);
				return entry1.GetTranslation (0).CompareTo (entry2.GetTranslation (0));
			});
			IdeApp.Workbench.StatusBar.ShowMessage (string.Format (GettextCatalog.GetPluralString ("Found {0} catalog entry.", "Found {0} catalog entries.", found), found));
			treeviewEntries.Model = store = newStore;
		}
		
		
			
		
		bool IsVisible (TreePath path)
		{
			TreePath start, end, cur;
			this.treeviewEntries.GetVisibleRange (out start, out end);
			TreeIter iter;
			if (!store.GetIter (out iter, start))
				return false;
			do {
				cur = store.GetPath (iter);
				if (cur.Equals (path))
					return true;
			} while (!cur.Equals (end) && store.IterNext (ref iter));
			return false;
		}
		
		public void SelectEntry (CatalogEntry entry)
		{
//			if (updateThread.IsBusy)
//				return;
			
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					CatalogEntry curEntry = store.GetValue (iter, 0) as CatalogEntry;
					if (entry == curEntry) {
						this.treeviewEntries.Selection.SelectIter (iter);
						TreePath iterPath = store.GetPath (iter);
						if (!IsVisible (iterPath))
							this.treeviewEntries.ScrollToCell (iterPath, treeviewEntries.GetColumn (0), true, 0, 0);
						return;
					}
				} while (store.IterNext (ref iter));
			}
			store.AppendValues (GetStockForEntry (entry), 
			                    entry.IsFuzzy,
			                    EscapeForTreeView (entry.String), 
			                    EscapeForTreeView (entry.GetTranslation (0)), 
			                    entry,
			                    GetRowColorForEntry (entry),
			                    GetTypeSortIndicator (entry),
			                    GetForeColorForEntry (entry)
			);
			SelectEntry (entry);
		}
		#endregion
		
#region Toolbar handling
		ToggleToolButton AddButton (string label)
		{
			ToggleToolButton newButton = new ToggleToolButton ();
			isUpdating = true;
			try {
				newButton.Label = label;
				newButton.IsImportant = true;
				newButton.Clicked += new EventHandler (OnButtonToggled);
				newButton.ShowAll ();
				this.toolbarPages.Insert (newButton, -1);
			} finally {
				isUpdating = false;
			}
			return newButton;
		}
		
		protected virtual void OnButtonToggled (object sender, System.EventArgs e)
		{
			int i = Array.IndexOf (this.toolbarPages.Children, sender);
			if (i != -1)
				ShowPage (i);
		}
		
		bool isUpdating = false;
		void ShowPage (int page)
		{
			if (notebookPages.CurrentPage == page || isUpdating)
				return;
				
			isUpdating = true;
			try {
				notebookPages.CurrentPage = page;
				for (int i = 0; i < toolbarPages.Children.Length; i++) {
					((ToggleToolButton) toolbarPages.Children[i]).Active = (i == page);
				}
			} finally {
				isUpdating = false;
			}
		}
#endregion
		
		protected override void OnDestroyed ()
		{
			MonoDevelop.Ide.Gui.Styles.Changed -= HandleStylesChanged;
			StopTaskWorkerThread ();
			
			widgets.Remove (this);
			ClearTasks ();
			
			base.OnDestroyed ();
		}
#region Tasks
		public class TranslationTask : TaskListEntry
		{
			POEditorWidget widget;
			CatalogEntry entry;
			
			public TranslationTask (POEditorWidget widget, CatalogEntry entry, string description) : base (widget.poFileName,
			                                                                           description, 0, 0,
			                                                                           TaskSeverity.Error, TaskPriority.Normal, null, widget)
			{
				this.widget = widget;
				this.entry  = entry;
			}
			
			public override void JumpToPosition ()
			{
				widget.SelectEntry (entry);
			}
		}
		
		void ClearTasks ()
		{
			TaskService.Errors.ClearByOwner (this);
		}
		
		static bool CompareTasks (List<TaskListEntry> list1, List<TaskListEntry> list2)
		{
			if (list1.Count != list2.Count)
				return false;
			for (int i = 0; i < list1.Count; i++) {
				if (list1[i].Description != list2[i].Description)
					return false;
			}
			return true;
		}
		static CatalogEntryRule[] allRules = {
			new PointRuleCatalogEntryRule (),
			new CaseMismatchCatalogEntryRule (),
			new UnderscoreCatalogEntryRule (),
			new StringFormatCatalogEntryRule (),
			new EndsWithWhitespaceCatalogEntryRule ()
		};
		IEnumerable<CatalogEntryRule> rules = new CatalogEntryRule[] {};
		
		public void UpdateRules (string country)
		{
			rules = from n in allRules where n.IsValid (country) select n;
			UpdateTasks ();
		}
		
		abstract class CatalogEntryRule
		{
			public virtual bool IsValid (string country)
			{
				switch (country) {
				case "ca":
				case "cs":
				case "da":
				case "de":
				case "es":
				case "fr":
				case "hu":
				case "it":
				case "nl":
				case "pl":
				case "pt":
					return true;
				}
				return false;
			}
			public abstract bool EntryFails (CatalogEntry entry);
			public abstract string FailReason (CatalogEntry entry);
		}
		
		class EndsWithWhitespaceCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return entry.String.EndsWith (" ") && !entry.GetTranslation (0).EndsWith (" ");
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				return GettextCatalog.GetString ("Translation for '{0}' doesn't end with whitespace ' '.", entry.String);
			}
		}
			
		class PointRuleCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return entry.String.EndsWith (".") && !entry.GetTranslation (0).EndsWith (".");
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				return GettextCatalog.GetString ("Translation for '{0}' doesn't end with '.'.", entry.String);
			}
		}
		
		class CaseMismatchCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return char.IsLetter (entry.String[0]) && char.IsLetter (entry.GetTranslation (0)[0])  &&
						char.IsUpper (entry.String[0]) && !char.IsUpper (entry.GetTranslation (0)[0]);
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				return GettextCatalog.GetString ("Casing mismatch in '{0}'", entry.String);
			}
		}
		
		class UnderscoreCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return entry.String.Contains ("_") && !entry.GetTranslation (0).Contains ("_") ||
					!entry.String.Contains ("_") && entry.GetTranslation (0).Contains ("_");
				
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				if (entry.String.Contains ("_") && !entry.GetTranslation (0).Contains ("_"))
					return GettextCatalog.GetString ("Original string '{0}' contains '_', translation doesn't.", entry.String);
				return GettextCatalog.GetString ("Original string '{0}' doesn't contain '_', translation does.", entry.String);
			}
		}
		
		class StringFormatCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches (entry.String, @"\{.\}", RegexOptions.None))  {
					if (!entry.GetTranslation (0).Contains (match.Value)) 
						return true;
				}
				return false;
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches (entry.String, @"\{.\}", RegexOptions.None))  {
					if (!entry.GetTranslation (0).Contains (match.Value)) 
						return GettextCatalog.GetString ("Original string '{0}' contains '{1}', translation doesn't.", entry.String, match.Value);
				}
				return "";
			}
		}
		
		
		List<TaskListEntry> currentTasks = new List<TaskListEntry> ();
		
		BackgroundWorker updateTaskThread = null;
		
		void TaskUpdateWorker (object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;
			if (catalog == null) {
				ClearTasks ();
				return;
			}
			
			List<TaskListEntry> tasks = new List<TaskListEntry> ();
			try {
				foreach (CatalogEntryRule rule in rules) {
					foreach (CatalogEntry entry in catalog) {
						if (worker != null && worker.CancellationPending)
							return;
						if (String.IsNullOrEmpty (entry.String) || String.IsNullOrEmpty (entry.GetTranslation (0)))
							continue;
							if (rule.EntryFails (entry)) {
								tasks.Add (new TranslationTask (this,
								                                entry,
								                                rule.FailReason (entry)));
							}
					}
				}
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
				return;
			}
			
			if (!CompareTasks (tasks, currentTasks)) {
				ClearTasks ();
				currentTasks = tasks;
				TaskService.Errors.AddRange (tasks);
			}
		}
		
		void StopTaskWorkerThread ()
		{
			updateTaskThread.CancelAsync ();
			int count = 0;
			while (count++ < 5 && updateTaskThread.IsBusy)  {
				Thread.Sleep (20);
			}
			updateTaskThread.Dispose ();
			updateTaskThread = new BackgroundWorker ();
			updateTaskThread.WorkerSupportsCancellation = true;
			updateTaskThread.DoWork += TaskUpdateWorker;
			
		}
		
		void UpdateTasks ()
		{
			StopTaskWorkerThread ();
			updateTaskThread.RunWorkerAsync ();
		}
#endregion

		#region IUndoHandler implementation
		Stack<Change> undoStack = new Stack<Change> ();
		Stack<Change> redoStack = new Stack<Change> ();
		public class Change
		{
			POEditorWidget widget;
			public CatalogEntry Entry {
				get; 
				set;
			}
			public string OldText {
				get;
				set;
			}
			public string Text {
				get;
				set;
			}
			public int Index {
				get;
				set;
			}
			public Change (POEditorWidget widget, CatalogEntry entry, string oldText, string text, int index)
			{
				this.widget = widget;
				this.Entry = entry;
				this.OldText = oldText;
				this.Text  = text;
				this.Index = index;
			}
			
			public void Undo ()
			{
				widget.inUndoOperation = true;
				widget.SelectEntry (Entry);
				var textView = widget.GetTextView (Index);
				if (textView != null)
					textView.Text = OldText;
				widget.inUndoOperation = false;
			}
			
			public void Redo ()
			{
				widget.inUndoOperation = true;
				widget.SelectEntry (Entry);
				var textView = widget.GetTextView (Index);
				if (textView != null)
					textView.Text = Text;
				widget.inUndoOperation = false;
			}
		}
		
		bool inUndoOperation = false;
		public void AddChange (CatalogEntry entry, string oldText, string text, int index)
		{
			if (inUndoOperation)
				return;
			redoStack.Clear ();
			undoStack.Push (new Change (this, entry, oldText, text, index));
		}
		
		public void Undo ()
		{
			Change change = undoStack.Pop ();
			change.Undo ();
			redoStack.Push (change);
		}
		
		public void Redo ()
		{
			Change change = redoStack.Pop ();
			change.Redo ();
			undoStack.Push (change);
		}
		
		class DisposeStub : IDisposable
		{
			public void Dispose ()
			{
			}
		}
		
		public IDisposable OpenUndoGroup ()
		{
			return new DisposeStub ();
		}
		
		public bool EnableUndo {
			get {
				return undoStack.Count > 0;
			}
		}
		
		public bool EnableRedo {
			get {
				return redoStack.Count > 0;
			}
		}
		#endregion
	}
}