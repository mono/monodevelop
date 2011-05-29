// 
// ColorShemeEditor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class ColorShemeEditor : Gtk.Dialog
	{
		TextEditor textEditor;
		Style colorSheme;
		Gtk.TreeStore colorStore = new Gtk.TreeStore (typeof (string), typeof (ChunkStyle));
		string fileName;
		HighlightingPanel panel;
		
		public ColorShemeEditor (HighlightingPanel panel)
		{
			this.panel = panel;
			this.Build ();
			textEditor = new TextEditor ();
			textEditor.Options = DefaultSourceEditorOptions.Instance;
			this.scrolledwindowTextEditor.Child = textEditor;
			textEditor.ShowAll ();
			
			this.treeviewColors.AppendColumn (GettextCatalog.GetString ("Name"), new Gtk.CellRendererText (), "text", 0);
			this.treeviewColors.Model = colorStore;
			this.treeviewColors.Selection.Changed += HandleTreeviewColorsSelectionChanged;
			this.colorbuttonFg.ColorSet += Stylechanged;
			this.colorbuttonBg.ColorSet += Stylechanged;
			this.checkbuttonBold.Toggled += Stylechanged;
			this.checkbuttonItalic.Toggled += Stylechanged;
			
			this.buttonOk.Clicked += HandleButtonOkClicked;
			HandleTreeviewColorsSelectionChanged (null, null);
			
		}

		void ApplyStyle (Style sheme)
		{
			sheme.Name = entryName.Text;
			sheme.Description = entryDescription.Text;
			
			Gtk.TreeIter iter;
			if (colorStore.GetIterFirst (out iter)) {
				do {
					var name  = (string)colorStore.GetValue (iter, 0);
					var style = (ChunkStyle)colorStore.GetValue (iter, 1);
					sheme.SetChunkStyle (name, style);
				} while (colorStore.IterNext (ref iter));
			}
		}

		void HandleButtonOkClicked (object sender, EventArgs e)
		{
			ApplyStyle (colorSheme);
			try {
				colorSheme.Save (fileName);
				panel.ShowStyles ();
			} catch (Exception ex) {
				MessageService.ShowException (ex);
			}
		}

		void Stylechanged (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			if (!this.treeviewColors.Selection.GetSelected (out iter))
				return;
			var chunkStyle = (ChunkStyle)colorStore.GetValue (iter, 1);
			ChunkProperties prop = ChunkProperties.None;
			if (checkbuttonBold.Active)
				prop |= ChunkProperties.Bold;
			if (checkbuttonItalic.Active)
				prop |= ChunkProperties.Italic;
			colorStore.SetValue (iter, 1, new ChunkStyle (colorbuttonFg.Color, colorbuttonBg.Color, prop));
			
			var newStyle = colorSheme.Clone ();
			ApplyStyle (newStyle);
			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newStyle;
			this.textEditor.QueueDraw ();
			
		}

		void HandleTreeviewColorsSelectionChanged (object sender, EventArgs e)
		{
			this.colorbuttonBg.Sensitive = false;
			this.colorbuttonFg.Sensitive = false;
			this.checkbuttonBold.Sensitive = false;
			this.checkbuttonItalic.Sensitive = false;
			
			Gtk.TreeIter iter;
			if (!this.treeviewColors.Selection.GetSelected (out iter))
				return;
			var chunkStyle = (ChunkStyle)colorStore.GetValue (iter, 1);
			colorbuttonFg.Color = chunkStyle.Color;
			colorbuttonBg.Color = chunkStyle.BackgroundColor;
			checkbuttonBold.Active = chunkStyle.Bold;
			checkbuttonItalic.Active = chunkStyle.Italic;
			
			this.colorbuttonBg.Sensitive = true;
			this.colorbuttonFg.Sensitive = true;
			this.checkbuttonBold.Sensitive = true;
			this.checkbuttonItalic.Sensitive = true;
		}
		
		public void SetSheme (Style style)
		{
			if (style == null)
				throw new ArgumentNullException ("style");
			this.fileName = Mono.TextEditor.Highlighting.SyntaxModeService.GetFileNameForStyle (style);
			this.colorSheme = style;
			this.entryName.Text = style.Name;
			this.entryDescription.Text = style.Description;
			this.textEditor.GetTextEditorData ().ColorStyle = style;
			foreach (var name in style.ColorNames) {
				colorStore.AppendValues (name, style.GetChunkStyle (name));
			}
			Stylechanged (null, null);
		}
	}
}

