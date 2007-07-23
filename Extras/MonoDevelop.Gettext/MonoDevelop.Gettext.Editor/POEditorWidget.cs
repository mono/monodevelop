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
using System.IO;

using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Gettext.Editor;

namespace MonoDevelop.Gettext
{
	public partial class POEditorWidget : Gtk.Bin
	{
		CatalogHeadersWidget headersEditor;
		ListStore store;
		ListStore foundInStore;
		Catalog catalog;
		string  poFileName;
		
		public Catalog Catalog {
			get {
				return catalog;
			}
			set {
				catalog = value;
				headersEditor.CatalogHeaders = catalog.Headers;
				if (GtkSpell.IsSupported) {
					GtkSpell.Attach (this.textviewOriginal, catalog.LocaleCode);
					GtkSpell.Attach (this.textviewOriginalPlural, catalog.LocaleCode);
				}
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
		
		public POEditorWidget()
		{
			this.Build();
			this.headersEditor = new CatalogHeadersWidget ();
			this.notebookPages.AppendPage (headersEditor, new Gtk.Label ());
		
			AddButton (GettextCatalog.GetString ("Translation")).Active = true;
			AddButton (GettextCatalog.GetString ("Headers")).Active = false;
			
			// entries tree view 
			store = new ListStore (typeof (string), typeof (bool), typeof (string), typeof (string), typeof (CatalogEntry), typeof (Gdk.Color));
			this.treeviewEntries.Model = store;
			
			treeviewEntries.AppendColumn (String.Empty, new CellRendererPixbuf (), "stock_id", Columns.Stock, "cell-background-gdk", Columns.RowColor);
			
			CellRendererToggle cellRendFuzzy = new CellRendererToggle ();
			cellRendFuzzy.Toggled += new ToggledHandler (FuzzyToggled);
			cellRendFuzzy.Activatable = true;
			treeviewEntries.AppendColumn (GettextCatalog.GetString ("Fuzzy"), cellRendFuzzy, "active", Columns.Fuzzy, "cell-background-gdk", Columns.RowColor);
			 
			CellRendererText original = new CellRendererText ();
			original.Ellipsize = Pango.EllipsizeMode.End;
			treeviewEntries.AppendColumn (GettextCatalog.GetString ("Original string"), original, "text", Columns.String, "cell-background-gdk", Columns.RowColor);
			
			CellRendererText translation = new CellRendererText ();
			translation.Ellipsize = Pango.EllipsizeMode.End;
			treeviewEntries.AppendColumn (GettextCatalog.GetString ("Translated string"), translation, "text", Columns.Translation, "cell-background-gdk", Columns.RowColor);
			treeviewEntries.Selection.Changed += new EventHandler (OnEntrySelected);
			
			// found in tree view
			foundInStore = new ListStore (typeof (string), typeof (string), typeof (string));
			this.treeviewFoundIn.Model = foundInStore;
			
			treeviewFoundIn.AppendColumn (GettextCatalog.GetString ("File"), new CellRendererText (), "text", FoundInColumns.File);
			treeviewFoundIn.AppendColumn (GettextCatalog.GetString ("Line"), new CellRendererText (), "text", FoundInColumns.Line);
			treeviewFoundIn.GetColumn (1).FixedWidth = 100;
			
			treeviewFoundIn.RowActivated += delegate (object sender, RowActivatedArgs e) {
				TreeIter iter;
				foundInStore.GetIter (out iter, e.Path); 				
				string line = foundInStore.GetValue (iter, (int)FoundInColumns.Line) as string;
				string file = foundInStore.GetValue (iter, (int)FoundInColumns.FullFileName) as string;
				int lineNr = 1;
				try {
					lineNr = 1 + Int32.Parse (line);
				} catch (Exception) {}
				IdeApp.Workbench.OpenDocument (file, lineNr, 1, true);
			};
			
			this.textviewTranslated.Buffer.Changed += delegate {
				if (this.isUpdating)
					return;
				this.currentEntry.SetTranslation (textviewTranslated.Buffer.Text, 0);
				UpdateProgressBar ();
			};
			
			this.textviewTranslatedPlural.Buffer.Changed += delegate {
				if (this.isUpdating)
					return;
				this.currentEntry.SetTranslation (textviewTranslatedPlural.Buffer.Text, 1);
				UpdateProgressBar ();
			};
			this.textviewComments.Buffer.Changed += delegate {
				if (this.isUpdating)
					return;
				string[] lines = textviewComments.Buffer.Text.Split (new string[] { Environment.NewLine }, StringSplitOptions.None);
				for (int i = 0; i < lines.Length; i++) {
					if (!lines[i].StartsWith ("#"))
						lines[i] = "# " + lines[i];
				}
				this.currentEntry.Comment = String.Join (Environment.NewLine, lines);
				UpdateProgressBar ();
			};
		}
		
		void UpdateProgressBar ()
		{
			int all, untrans, fuzzy, missing, bad;
			catalog.GetStatistics (out all, out fuzzy, out missing, out bad, out untrans);
			double percentage = all > 0 ? ((double)(all - untrans) / all) * 100 : 0.0;
			string barText = String.Format (GettextCatalog.GetString ("{0:#00.00}% Translated"), percentage);
			
			if (missing > 0 || fuzzy > 0)
				barText += " (";
			
			if (fuzzy > 0) {
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Fuzzy Message", "{0} Fuzzy Messages", fuzzy), fuzzy);
			}
			
			if (missing > 0) {
				if (fuzzy > 0) {
					barText += ", ";
				}
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Missing Message", "{0} Missing Messages", missing), missing);
			}
			if (missing > 0 || fuzzy > 0)
				barText += ")";
			
			this.progressbar1.Text = barText;
			percentage = percentage / 100;
			this.progressbar1.Fraction = percentage;
		}		
		
#region EntryEditor handling
		CatalogEntry currentEntry;
		
		void EditEntry (CatalogEntry entry)
		{
			this.isUpdating = true;
			try {
				currentEntry = entry;
				this.textviewOriginal.Buffer.Text = entry.String;
				this.textviewTranslated.Buffer.Text = entry.GetTranslation (0);
				
				frameOriginalPlural.Visible = frameTranslatedPlural.Visible = entry.HasPlural;
				
				
				if (entry.HasPlural) {
					this.textviewOriginalPlural.Buffer.Text = entry.PluralString;
					this.textviewTranslatedPlural.Buffer.Text = entry.GetTranslation (1);
				}
				
				this.foundInStore.Clear ();
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
					this.foundInStore.AppendValues (file, line, fullName);
				}
				this.textviewComments.Buffer.Text = entry.Comment;
			} finally {
				this.isUpdating = false;
			}
		}
		
#endregion
		
#region TreeView handling
		enum Columns : int
		{
			Stock,
			Fuzzy,
			String,
			Translation,
			CatalogEntry,
			RowColor,
			Count
		}
		
		enum FoundInColumns : int
		{
			File,
			Line,
			FullFileName
		}
		
		void FuzzyToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				bool val = (bool)store.GetValue (iter, (int)Columns.Fuzzy);
				CatalogEntry entry = (CatalogEntry)store.GetValue (iter, (int)Columns.CatalogEntry);
				entry.IsFuzzy = !val;
				store.SetValue (iter, (int)Columns.Fuzzy, !val);
				store.SetValue (iter, (int)Columns.Stock, GetStockForEntry (entry));
				store.SetValue (iter, (int)Columns.RowColor, GetRowColorForEntry (entry));
				UpdateProgressBar ();
			}
		}
		
		static string GetStockForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return Stock.DialogError;
			return entry.IsFuzzy ? Stock.About : entry.IsTranslated ? Stock.Apply : Stock.Cancel;
		}
		
		static Color translated   = new Color (255, 255, 255);
		static Color untranslated = new Color (234, 232, 227);
		static Color fuzzy        = new Color (237, 226, 187);
		static Color missing      = new Color (237, 167, 167);
		
		static Color GetRowColorForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return missing;
			return entry.IsFuzzy ? fuzzy : entry.IsTranslated ? translated : untranslated;
		}
			
		void OnEntrySelected (object sender, EventArgs args)
		{			
			TreeIter iter;
			if (treeviewEntries.Selection.GetSelected (out iter)) {
				CatalogEntry entry = store.GetValue (iter, (int)Columns.CatalogEntry) as CatalogEntry;
				if (entry != null)
					EditEntry (entry);
			}
		}
		
		public void UpdateEntry (CatalogEntry entry)
		{	
			TreeIter iter, foundIter = TreeIter.Zero;
			
			// Look if selected is the same - only wanted usecase
			if (treeviewEntries.Selection.GetSelected (out iter)) {
				CatalogEntry storeEntry = store.GetValue (iter, (int)Columns.CatalogEntry) as CatalogEntry;
				if (entry.Equals (storeEntry))
					foundIter = iter;
			}
						
			// Update data
			if (foundIter.Stamp != TreeIter.Zero.Stamp) {
				store.SetValue (foundIter, (int)Columns.Fuzzy, entry.IsFuzzy);
				store.SetValue (foundIter, (int)Columns.Stock, GetStockForEntry (entry));
				store.SetValue (foundIter, (int)Columns.RowColor, GetRowColorForEntry (entry));
			}
		}
		
		void UpdateFromCatalog ()
		{
			store.Clear ();
			foreach (CatalogEntry entry in catalog) {
				store.AppendValues (GetStockForEntry (entry), entry.IsFuzzy, entry.String, entry.GetTranslation (0), entry, GetRowColorForEntry (entry));
			}
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
		
		public override void Dispose ()
		{
			this.headersEditor.Destroy ();
			this.headersEditor = null;
			
			base.Dispose ();
		}
	}
}
