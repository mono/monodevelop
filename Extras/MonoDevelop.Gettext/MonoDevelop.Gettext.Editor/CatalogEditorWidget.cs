//
// CatalogEditorWidget.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2007 David Makovský
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
using Gtk;
using Gdk;

namespace MonoDevelop.Gettext.Editor
{
	class EditorWidget : VBox
	{
		Catalog catalog;
		CatalogEntryEditorWidget entryEditor;
		ListStore store;
		TreeView tv;
		VPaned vpanedMain;

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
		
		public EditorWidget (Catalog catalog)
		{
			this.catalog = catalog;
			
			store = new ListStore (typeof (string), typeof (bool), typeof (string), typeof (string), typeof (CatalogEntry), typeof (Gdk.Color));
			UpdateFromCatalog ();
			
			ScrolledWindow sw = new ScrolledWindow ();
			sw.VscrollbarPolicy = PolicyType.Automatic;
			sw.HscrollbarPolicy = PolicyType.Automatic;
			
			tv = new TreeView (store);

			tv.HeadersVisible = true;
			tv.AppendColumn (String.Empty, new CellRendererPixbuf (), "stock_id", Columns.Stock, "cell-background-gdk", Columns.RowColor);
			
			CellRendererToggle cellRendFuzzy = new CellRendererToggle ();
			cellRendFuzzy.Toggled += new ToggledHandler (FuzzyToggled);
			cellRendFuzzy.Activatable = true;
			tv.AppendColumn ("Fuzzy", cellRendFuzzy, "active", Columns.Fuzzy, "cell-background-gdk", Columns.RowColor);
			
			CellRendererText original = new CellRendererText ();
			original.Ellipsize = Pango.EllipsizeMode.End;
			tv.AppendColumn ("Original string", original, "text", Columns.String, "cell-background-gdk", Columns.RowColor);
			
			CellRendererText translation = new CellRendererText ();
			translation.Ellipsize = Pango.EllipsizeMode.End;
			tv.AppendColumn ("Translated string", translation, "text", Columns.Translation, "cell-background-gdk", Columns.RowColor);
			
			tv.Selection.Changed += new EventHandler (OnEntrySelected);
			
			sw.Add (tv);
			
			vpanedMain = new VPaned ();
			vpanedMain.Add1 (sw);
			
			//vpanedMain.Position = 400;
			
			entryEditor = new CatalogEntryEditorWidget (catalog.PluralFormsDescriptions, catalog.LocaleCode);
			vpanedMain.Add2 (entryEditor);
			
			this.Add (vpanedMain);
		}
		
		public Catalog Catalog
		{
			get { return catalog; }
			set
			{
				catalog = value;
				UpdateFromCatalog ();
				entryEditor.SetPluralDescriptions (catalog.PluralFormsDescriptions, catalog.LocaleCode);
			}
		}
		
		public void UpdateEntry (CatalogEntry entry)
		{	
			TreeIter iter, foundIter = TreeIter.Zero;
			
			// Look if selected is the same - only wanted usecase
			if (tv.Selection.GetSelected (out iter))
			{
				CatalogEntry storeEntry = store.GetValue (iter, (int)Columns.CatalogEntry) as CatalogEntry;
				if (entry.Equals (storeEntry))
				{
					foundIter = iter;
				}
			}
						
			// Update data
			if (foundIter.Stamp != TreeIter.Zero.Stamp)
			{
				store.SetValue (foundIter, (int)Columns.Fuzzy, entry.IsFuzzy);
				store.SetValue (foundIter, (int)Columns.Stock, GetStockForEntry (entry));
				store.SetValue (foundIter, (int)Columns.RowColor, GetRowColorForEntry (entry));
			}
		}
		
		void UpdateEntries ()
		{
			TreeIter iter;
			store.GetIterFirst (out iter);
			do
			{
				CatalogEntry entry = store.GetValue (iter, (int)Columns.CatalogEntry) as CatalogEntry;
				if (entry != null)
				{
					store.SetValue (iter, (int)Columns.Fuzzy, entry.IsFuzzy);
					store.SetValue (iter, (int)Columns.Stock, GetStockForEntry (entry));
					store.SetValue (iter, (int)Columns.RowColor, GetRowColorForEntry (entry));
				}
			} while (store.IterNext (ref iter));
		}
		         
		public void UpdatePluralDefinitions ()
		{
			entryEditor.SetPluralDescriptions (catalog.PluralFormsDescriptions, catalog.LocaleCode);
			if (tv.Selection != null)
			{
				TreeIter iter;
				tv.Selection.GetSelected (out iter);
				tv.Selection.UnselectIter (iter);
				tv.Selection.SelectIter (iter);
			}
			UpdateEntries ();
		}
		
		void UpdateFromCatalog ()
		{
			store.Clear ();
			foreach (CatalogEntry entry in catalog)
			{
				store.AppendValues (GetStockForEntry (entry), entry.IsFuzzy, entry.String, entry.GetTranslation (0), entry, GetRowColorForEntry (entry));
			}
		}
		
		void FuzzyToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path))
			{
				bool val = (bool)store.GetValue (iter, (int)Columns.Fuzzy);
				CatalogEntry entry = (CatalogEntry)store.GetValue (iter, (int)Columns.CatalogEntry);
				entry.IsFuzzy = !val;
				store.SetValue (iter, (int)Columns.Fuzzy, !val);
				store.SetValue (iter, (int)Columns.Stock, GetStockForEntry (entry));
				store.SetValue (iter, (int)Columns.RowColor, GetRowColorForEntry (entry));
			}
		}
		
		static string GetStockForEntry (CatalogEntry entry)
		{
			return entry.IsFuzzy ? Stock.About : entry.IsTranslated ? Stock.Apply : Stock.Cancel;
		}
		
		// TODO: make colors configurable?
		static Color translated = new Color (255, 255, 255);
		static Color untranslated = new Color (234, 232, 227);
		static Color fuzzy = new Color (237, 226, 187);
		
		static Color GetRowColorForEntry (CatalogEntry entry)
		{
			return entry.IsFuzzy ? fuzzy : entry.IsTranslated ? translated : untranslated;
		}
		
		void OnEntrySelected (object sender, EventArgs args)
		{			
			TreeIter iter;
			if (tv.Selection.GetSelected (out iter))
			{
				CatalogEntry entry = store.GetValue (iter, (int)Columns.CatalogEntry) as CatalogEntry;
				if (entry != null)
				{
					entryEditor.Entry = entry;
				}
			}
		}
	}
}
