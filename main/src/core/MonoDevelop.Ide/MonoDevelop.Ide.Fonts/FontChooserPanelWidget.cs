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
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Ide.Fonts
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class FontChooserPanelWidget : Gtk.Bin
	{
		TreeStore fontStore;
		ListStore comboBoxStore;
		
		Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
		Gtk.CellRendererCombo comboRenderer = new Gtk.CellRendererCombo ();
		
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
					FontService.SetFont (fontName, FontService.GetFont (fontName).FontDescription);
					return;
				}
				var selection = new FontSelectionDialog (GettextCatalog.GetString ("Select Font"));
				
				if (MessageService.ShowCustomDialog (selection) == (int)Gtk.ResponseType.Ok) {
					FontService.SetFont (fontName, selection.FontName);
					fontStore.SetValue (iter, 2, selection.FontName);
				}
				selection.Destroy ();
			};
  
			var fontCol = new TreeViewColumn ();
			
			comboRenderer.HasEntry = false;
			comboRenderer.Mode = CellRendererMode.Activatable;
			comboRenderer.TextColumn = 0;
			comboRenderer.Editable = true;
			fontCol.PackStart (comboRenderer, true);
			
			fontCol.SetCellDataFunc (comboRenderer, delegate (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) {
				string fontValue = (string)fontStore.GetValue (iter, colValue);
				string fontName = (string)fontStore.GetValue (iter, colName);
				var d = FontService.GetFont (fontName);
				comboBoxStore.Clear ();
				if (d == null || d.FontDescription != fontValue)
					comboBoxStore.AppendValues (fontValue);
				comboBoxStore.AppendValues (GettextCatalog.GetString ("Default"));
				comboBoxStore.AppendValues (GettextCatalog.GetString ("Edit..."));
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
				fontStore.AppendValues (GettextCatalog.GetString (desc.DisplayName), FontService.GetFontDescriptionName (desc.Name), desc.Name);
			}
		}
		
		public void Store ()
		{
		}
	}
}

