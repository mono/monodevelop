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
using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class ColorShemeEditor : Gtk.Dialog
	{
		MonoTextEditor textEditor;
		ColorScheme colorSheme;
		TreeStore colorStore = new Gtk.TreeStore (typeof (string), typeof(ColorScheme.PropertyDescription), typeof(object));
		string fileName;
		HighlightingPanel panel;

		public ColorShemeEditor (HighlightingPanel panel)
		{
			this.panel = panel;
			this.Build ();
			textEditor = new MonoTextEditor ();
			textEditor.Options = new StyledSourceEditorOptions (MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance);
			this.scrolledwindowTextEditor.Child = textEditor;
			textEditor.ShowAll ();
			
			this.treeviewColors.AppendColumn (GettextCatalog.GetString ("Name"), new Gtk.CellRendererText (), new CellLayoutDataFunc (SyntaxCellRenderer));
			this.treeviewColors.HeadersVisible = false;
			this.treeviewColors.Model = colorStore;
			this.treeviewColors.SearchColumn = -1; // disable the interactive search
			this.treeviewColors.Selection.Changed += HandleTreeviewColorsSelectionChanged;
			this.hpaned1.Position = 250;
			this.SetSizeRequest (1024, 768);

			this.colorbuttonFg.ColorSet += Stylechanged;
			this.colorbuttonBg.ColorSet += Stylechanged;
			this.colorbuttonPrimary.ColorSet += Stylechanged;
			this.colorbuttonSecondary.ColorSet += Stylechanged;
			this.colorbuttonBorder.ColorSet += Stylechanged;
			colorbuttonBg.UseAlpha = true;
			this.checkbuttonBold.Toggled += Stylechanged;
			this.checkbuttonItalic.Toggled += Stylechanged;
			
			this.buttonOk.Clicked += HandleButtonOkClicked;
			HandleTreeviewColorsSelectionChanged (null, null);
			notebookColorChooser.ShowTabs = false;
		}

		void SyntaxCellRenderer (Gtk.CellLayout cell_layout, Gtk.CellRenderer cell, Gtk.TreeModel tree_model, Gtk.TreeIter iter)
		{
			var renderer = (Gtk.CellRendererText)cell;
			var data = (ColorScheme.PropertyDescription)colorStore.GetValue (iter, 1);
			string markup = GLib.Markup.EscapeText (data.Attribute.Name);
			renderer.Markup = markup;
		}

		void ApplyStyle (ColorScheme sheme)
		{
			sheme.Name = entryName.Text;
			sheme.Description = entryDescription.Text;
			
			Gtk.TreeIter iter;
			if (colorStore.GetIterFirst (out iter)) {
				do {
					var data = (ColorScheme.PropertyDescription)colorStore.GetValue (iter, 1);
					var style = colorStore.GetValue (iter, 2);
					data.Info.SetValue (sheme, style, null);
				} while (colorStore.IterNext (ref iter));
			}
		}

		public static void RefreshAllColors ()
		{
			foreach (var doc in Ide.IdeApp.Workbench.Documents) {
				var editor = doc.Editor;
				if (editor == null)
					continue;
				doc.UpdateParseDocument ();
//				editor.Parent.TextViewMargin.PurgeLayoutCache ();
//				editor.Document.CommitUpdateAll ();
			}
		
		}

		void HandleButtonOkClicked (object sender, EventArgs e)
		{
			ApplyStyle (colorSheme);
			try {
				if (fileName.EndsWith (".vssettings", StringComparison.Ordinal)) {
					System.IO.File.Delete (fileName);
					fileName += "Style.json";
				}
				colorSheme.Save (fileName);
				panel.ShowStyles ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
			RefreshAllColors ();
		}


		void Stylechanged (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			if (!this.treeviewColors.Selection.GetSelected (out iter))
				return;

			var o = colorStore.GetValue (iter, 2);

			if (o is ChunkStyle) {
				SetChunkStyle (iter, (ChunkStyle)o);
			} else if (o is AmbientColor) {
				SetAmbientColor (iter, (AmbientColor)o);
			}
		}

		Cairo.Color GetColorFromButton (ColorButton button)
		{
			return new Cairo.Color (button.Color.Red / (double)ushort.MaxValue, button.Color.Green / (double)ushort.MaxValue, button.Color.Blue / (double)ushort.MaxValue, button.Alpha / (double)ushort.MaxValue);
		}

		void SetAmbientColor (Gtk.TreeIter iter, AmbientColor oldStyle)
		{
			var newStyle = new AmbientColor ();
			newStyle.Color = GetColorFromButton (colorbuttonPrimary);
			newStyle.SecondColor = GetColorFromButton (colorbuttonSecondary);

			colorStore.SetValue (iter, 2, newStyle);

			var newscheme = colorSheme.Clone ();
			ApplyStyle (newscheme);

			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newscheme;
			this.textEditor.QueueDraw ();
		}

		void SetChunkStyle (Gtk.TreeIter iter, ChunkStyle oldStyle)
		{
			var newStyle = new ChunkStyle (oldStyle);
			newStyle.Foreground = GetColorFromButton (colorbuttonFg);
			newStyle.Background =GetColorFromButton (colorbuttonBg);

			if (checkbuttonBold.Active) {
				newStyle.FontWeight = Xwt.Drawing.FontWeight.Bold;
			} else {
				newStyle.FontWeight = Xwt.Drawing.FontWeight.Normal;
			}

			if (checkbuttonItalic.Active) {
				newStyle.FontStyle = Xwt.Drawing.FontStyle.Italic;
			} else {
				newStyle.FontStyle = Xwt.Drawing.FontStyle.Normal;
			}

			colorStore.SetValue (iter, 2, newStyle);

			var newscheme = colorSheme.Clone ();
			ApplyStyle (newscheme);

			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newscheme;
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
			var o = colorStore.GetValue (iter, 2);
			if (o is ChunkStyle)
				SelectChunkStyle (iter, (ChunkStyle)o);

			if (o is AmbientColor)
				SelectAmbientColor (iter, (AmbientColor)o);
		}

		void SetColorToButton (ColorButton button, Cairo.Color color)
		{
			button.Color = (HslColor)color;
			button.Alpha = (ushort)(color.A * ushort.MaxValue);
		}

		void SelectAmbientColor (TreeIter iter, AmbientColor ambientColor)
		{
			notebookColorChooser.Page = 1;
			SetColorToButton (colorbuttonPrimary, ambientColor.Color);
			SetColorToButton (colorbuttonSecondary, ambientColor.SecondColor);
			colorbuttonSecondary.Sensitive = ambientColor.HasSecondColor;
			SetColorToButton (colorbuttonBorder, ambientColor.BorderColor);
			colorbuttonBorder.Sensitive = ambientColor.HasBorderColor;
		}

		void SelectChunkStyle (TreeIter iter, ChunkStyle chunkStyle)
		{
			notebookColorChooser.Page = 0;
			SetColorToButton (colorbuttonFg, chunkStyle.Foreground);
			SetColorToButton (colorbuttonBg, chunkStyle.Background);

			checkbuttonBold.Active = chunkStyle.FontWeight == Xwt.Drawing.FontWeight.Bold;
			checkbuttonItalic.Active = chunkStyle.FontStyle == Xwt.Drawing.FontStyle.Italic;
			this.label4.Visible = this.colorbuttonFg.Visible = true;
			this.colorbuttonFg.Sensitive = true;
			this.label5.Visible = this.colorbuttonBg.Visible = true;
			this.colorbuttonBg.Sensitive = true;
			this.checkbuttonBold.Visible = true;
			this.checkbuttonBold.Sensitive = true;
			this.checkbuttonItalic.Visible = true;
			this.checkbuttonItalic.Sensitive = true;
		}

		public void SetSheme (ColorScheme style)
		{
			if (style == null)
				throw new ArgumentNullException ("style");
			this.fileName = style.FileName;
			this.colorSheme = style;
			this.entryName.Text = style.Name;
			this.entryDescription.Text = style.Description;
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = style;
			this.textEditor.Text = @"using System;

// This is an example
class Example
{
	public static void Main (string[] args)
	{
		Console.WriteLine (""Hello World"");
	}
}";
			foreach (var data in ColorScheme.TextColors) {
				colorStore.AppendValues (data.Attribute.Name, data, data.Info.GetValue (style, null));
			}
			foreach (var data in ColorScheme.AmbientColors) {
				colorStore.AppendValues (data.Attribute.Name, data, data.Info.GetValue (style, null));
			}
			Stylechanged (null, null);
			
		}
	}
}

