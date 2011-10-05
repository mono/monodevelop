// 
// FontChooserPanelWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Ide.Fonts
{
	public partial class FontChooserPanelWidget : Gtk.Bin
	{
		TreeStore fontStore;
		ListStore comboBoxStore;
		
		Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
		Gtk.CellRendererCombo comboRenderer = new Gtk.CellRendererCombo ();
		Dictionary<string, string> customFonts = new Dictionary<string, string> ();
		
		
		public void SetFont (string fontName, string fontDescription)
		{
			customFonts [fontName] = fontDescription;
		}
		
		
		public string GetFont (string fontName)
		{
			if (customFonts.ContainsKey (fontName))
				return customFonts [fontName];
			
			return FontService.GetUnderlyingFontName (fontName);
		}

		public FontChooserPanelWidget ()
		{
			this.Build ();
			fontStore = new TreeStore (typeof (string), typeof (string), typeof (string));
			treeviewFonts.Model = fontStore;
			
			treeviewFonts.AppendColumn (GettextCatalog.GetString ("Name"), textRenderer, "text", colDisplayName);
			
			comboRenderer.Edited += delegate(object o, Gtk.EditedArgs args) {
				TreeIter iter;
				if (!fontStore.GetIterFromString (out iter, args.Path))
					return;
				string fontName = (string)fontStore.GetValue (iter, colName);
				
				if (args.NewText == GettextCatalog.GetString ("Default")) { 
					SetFont (fontName, FontService.GetFont (fontName).FontDescription);
					fontStore.SetValue (iter, colValue, GettextCatalog.GetString ("Default"));
					return;
				}
				var selectionDialog = new FontSelectionDialog (GettextCatalog.GetString ("Select Font")) {
					Modal = true,
					DestroyWithParent = true,
					TransientFor = this.Toplevel as Gtk.Window
				};
				try {
					string fontValue = FontService.FilterFontName (GetFont (fontName));
					selectionDialog.SetFontName (fontValue);
					if (MessageService.RunCustomDialog (selectionDialog) != (int) Gtk.ResponseType.Ok) {
						return;
					}
					fontValue = selectionDialog.FontName;
					if (fontValue ==  FontService.FilterFontName (FontService.GetFont (fontName).FontDescription))
						fontValue = FontService.GetFont (fontName).FontDescription;
					SetFont (fontName, fontValue);
					fontStore.SetValue (iter, colValue, selectionDialog.FontName);
				} finally {
					selectionDialog.Destroy ();
				}
			};
			
			comboRenderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				TreeIter iter;
				if (!fontStore.GetIterFromString (out iter, args.Path))
					return;
				string fontName = (string)fontStore.GetValue (iter, colName);
				string fontValue = GetFont (fontName);
				comboBoxStore.Clear ();
				if (fontValue != FontService.GetFont (fontName).FontDescription) 
					comboBoxStore.AppendValues (fontValue);
				
				comboBoxStore.AppendValues (GettextCatalog.GetString ("Default"));
				comboBoxStore.AppendValues (GettextCatalog.GetString ("Edit..."));
			};
			
			var fontCol = new TreeViewColumn ();
			fontCol.Title = GettextCatalog.GetString ("Font");
			
			comboRenderer.HasEntry = false;
			comboRenderer.Mode = CellRendererMode.Activatable;
			comboRenderer.TextColumn = 0;
			comboRenderer.Editable = true;
			fontCol.PackStart (comboRenderer, true);
			fontCol.SetCellDataFunc (comboRenderer, delegate (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) {
				string fontValue = (string)fontStore.GetValue (iter, colValue);
				string fontName = (string)fontStore.GetValue (iter, colName);
				var d = FontService.GetFont (fontName);
				if (d == null || d.FontDescription != fontValue) {
					comboRenderer.Text = fontValue;
				} else {
					comboRenderer.Text = GettextCatalog.GetString ("Default");
				}
			});
			
			treeviewFonts.AppendColumn (fontCol);
			
			comboBoxStore = new ListStore (typeof (string));
			
			comboRenderer.Model = comboBoxStore;
			
			LoadFonts ();
		}
		
		const int colDisplayName = 0;
		const int colValue       = 1;
		const int colName        = 2;
		
		public void LoadFonts ()
		{
			foreach (var desc in FontService.FontDescriptions) {
				fontStore.AppendValues (GettextCatalog.GetString (desc.DisplayName), FontService.GetUnderlyingFontName (desc.Name), desc.Name);
			}
		}
		
		public void Store ()
		{
			foreach (var val in customFonts) {
				FontService.SetFont (val.Key, val.Value);
			}
		}
	}
}

