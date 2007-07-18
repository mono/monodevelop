//
// CatalogEditor.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Gettext.Editor
{
	internal class CatalogEditor : AbstractViewContent
	{
		Catalog catalog = new Catalog ();
		POEditorWidget poEditorWidget = new POEditorWidget ();
		EditorWidget catalogEditor;
		CatalogHeadersWidget headersEditor;
		
		Notebook notebook;
		VBox box, vbox;
		HBox hbox;
		Toolbar toolbar;
		ProgressBar statBar;
		
		bool updating;
		
		private CatalogEditor ()
		{
//			catalogEditor = new EditorWidget (catalog);
			
//			headersEditor.PluralDefinitionChanged += delegate
//			{
//				catalogEditor.UpdatePluralDefinitions ();
//			};
//			poEditorWidget.Showll ();
//			//catalogEditor.ShowAll ();
//			headersEditor.ShowAll ();
//			
//			notebook = new Gtk.Notebook ();
//			
//			// Main notebook
//			notebook.TabPos = Gtk.PositionType.Bottom;
//			notebook.ShowTabs = false;
//			notebook.Show ();
//			box = new VBox ();
//			
//			// Bottom box - toolbar + progress bar
//			hbox = new HBox ();
//			
//			toolbar = new Toolbar ();
////			toolbar.IconSize = IconSize.SmallToolbar;
//			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
//			toolbar.ShowArrow = false;
//			
//			AddButton (GettextCatalog.GetString ("Translation"), catalogEditor).Active = true;
//			AddButton (GettextCatalog.GetString ("Headers"), headersEditor).Active = false;
//			toolbar.ShowAll ();
//			
//			statBar = new ProgressBar ();
//			statBar.Orientation = ProgressBarOrientation.LeftToRight;
//			string barText = String.Format (GettextCatalog.GetString ("{0:#00.00}% Translated"), 0.0) + " (";
//			barText += String.Format (GettextCatalog.GetPluralString ("{0} Fuzzy Message", "{0} Fuzzy Messages", 0), 0) + ")";
//			statBar.Text = barText;
//			statBar.Show ();
//			
//			vbox = new VBox ();
//			vbox.PackStart (statBar, false, false, 2);
//			vbox.Show ();
//			
//			hbox.PackStart (toolbar, true, true, 0);
//			hbox.PackStart (vbox, false, false, 4); 
//			
//			hbox.Show ();
//			
//			box.PackStart (notebook, true, true, 0);
//			box.PackStart (hbox, false, false, 0);
//			
//			box.Show ();
		}
		
		public CatalogEditor (string poFile) : this ()
		{
			Load (poFile);
			catalog.OnDirtyChanged += delegate (object sender, EventArgs args) {
				IsDirty = catalog.IsDirty;
				if (sender is CatalogEntry)
					this.poEditorWidget.UpdateEntry (sender as CatalogEntry);
			};
			
//			catalogEditor.ShowAll ();
//			headersEditor.ShowAll ();
		}
//		
//		ToggleToolButton AddButton (string label, Gtk.Widget page)
//		{
//			updating = true;
//			ToggleToolButton button = new ToggleToolButton ();
//			button.Label = label;
//			button.IsImportant = true;
//			button.Clicked += new EventHandler (OnButtonToggled);
//			button.ShowAll ();
//			toolbar.Insert (button, -1);
//			notebook.AppendPage (page, new Gtk.Label ());
//			updating = false;
//			return button;
//		}
//		
//		void RemoveButton (int npage)
//		{
//			notebook.RemovePage (npage);
//			toolbar.Remove (toolbar.Children [npage]);
//			ShowPage (0);
//		}
		
//		void OnButtonToggled (object s, EventArgs args)
//		{
//			int i = Array.IndexOf (toolbar.Children, s);
//			if (i != -1)
//				ShowPage (i);
//		}
//		
//		void ShowPage (int npage)
//		{
//			if (notebook.CurrentPage == npage)
//				return;
//				
//			if (updating) return;
//			updating = true;
//			
//			notebook.CurrentPage = npage;
//			Gtk.Widget[] buttons = toolbar.Children;
//			for (int n=0; n<buttons.Length; n++) {
//				ToggleToolButton b = (ToggleToolButton) buttons [n];
//				b.Active = (n == npage);
//			}
//
//			updating = false;
//		}
		
		public override void Dispose ()
		{
			this.poEditorWidget.Destroy ();
			this.poEditorWidget = null;
			
//			catalogEditor.Destroy ();
//			statBar.Destroy ();
//			vbox.Destroy ();
//			hbox.Destroy ();
//			box.Destroy ();
//			vbox = null;
//			hbox = null;
//			box = null;
			base.Dispose ();
		}
		
		public override void Load (string fileName)
		{
			catalog.Load (fileName);
			if (!catalog.IsOk)
			{
				// TODO: GUI Feedback
			}
			poEditorWidget.Catalog = catalog;
			poEditorWidget.POFileName = fileName;
			
//			catalogEditor.Catalog = catalog;
//			headersEditor.CatalogHeaders = catalog.Headers;
			
			this.ContentName = fileName;
			this.IsDirty = false;
		}
		
		public override void Save (string fileName)
		{
			OnBeforeSave (EventArgs.Empty);
			catalog.Save (fileName);
			ContentName = fileName;
			IsDirty = false;
		}
		
		public override void Save ()
		{
			Save (this.ContentName);
		}
		
		public override Widget Control
		{
			get { return poEditorWidget; }
		}
				
		public override bool IsReadOnly
		{
			get { return false; }
		}
		
		public override string TabPageLabel 
		{
			get { return GettextCatalog.GetString ("Gettext Editor"); }
		}
	}
}
