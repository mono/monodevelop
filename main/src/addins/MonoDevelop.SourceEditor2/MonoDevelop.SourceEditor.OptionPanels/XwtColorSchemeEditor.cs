//
// XwtColorSchemeEditor.cs
//
// Author:
//       Aleksandr Shevchenko <alexandre.shevchenko@gmail.com>
//
// Copyright (c) 2014 Aleksandr Shevchenko
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
using Xwt;
using Xwt.Drawing;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class XwtColorSchemeEditor:Dialog
	{
		TextEditor textEditor;
		ColorScheme colorSheme;
		DataField<string> nameField = new DataField<string> ();
		DataField<object> styleField = new DataField<object> ();
		DataField<ColorScheme.PropertyDecsription> propertyField = new DataField<ColorScheme.PropertyDecsription> ();
		TreeStore colorStore;
		string fileName;
		HighlightingPanel panel;
		TreeView treeviewColors = new TreeView ();
		LabeledColorButton colorbuttonPrimary = new LabeledColorButton ("Primary color:");
		LabeledColorButton colorbuttonSecondary = new LabeledColorButton ("Secondary color:");
		LabeledColorButton colorbuttonBorder = new LabeledColorButton ("Border color:");
		ToggleButton togglebuttonBold = new ToggleButton ("Bold");
		ToggleButton togglebuttonItalic = new ToggleButton ("Italic");
		Button buttonFormat = new Button ("Format by pattern");
		TextEntry entryName = new TextEntry ();
		TextEntry entryDescription = new TextEntry ();

		public XwtColorSchemeEditor (HighlightingPanel panel)
		{
			this.panel = panel;

			this.Buttons.Add (new DialogButton (Command.Cancel));
			this.Buttons.Add (new DialogButton (Command.Ok));

			colorStore = new TreeStore (nameField, styleField, propertyField);

			var mainTable = new Table ();

			var schemeTable = new Table ();
			schemeTable.Add (new Label () { Text="Name:" }, 0, 0);
			schemeTable.Add (entryName, 1, 0);
			schemeTable.Add (new Label () { Text="Description:" }, 2, 0);
			schemeTable.Add (entryDescription, 3, 0);

			mainTable.Add (schemeTable, 0, 0);

			var table = new Table ();

			table.Add (new TextEntry (), 0, 0);

			treeviewColors = new TreeView ();
			treeviewColors.Columns.Add (GettextCatalog.GetString ("Name"), nameField);
			this.treeviewColors.HeadersVisible = false;
			this.treeviewColors.DataSource = colorStore;
			this.treeviewColors.SelectionChanged += HandleTreeviewColorsSelectionChanged;

			table.Add (treeviewColors, 0, 1, 2, 1, true, true);
			table.Add (new VSeparator (), 1, 0, 3);

			var commandHBox = new HBox ();
			commandHBox.PackStart (new Button ("Undo"));
			commandHBox.PackStart (new Button ("Redo"));
			commandHBox.PackStart (new Button ("AutoSet"));
			table.Add (commandHBox, 2, 0);

			var adjustHBox = new HBox ();
			adjustHBox.PackStart (colorbuttonPrimary);
			adjustHBox.PackStart (colorbuttonSecondary);
			adjustHBox.PackStart (colorbuttonBorder);
			this.colorbuttonPrimary.ColorSet += Stylechanged;
			this.colorbuttonSecondary.ColorSet += Stylechanged;
			this.colorbuttonBorder.ColorSet += Stylechanged;
			//colorbuttonBg.UseAlpha = true;
			this.togglebuttonBold.Toggled += Stylechanged;
			this.togglebuttonItalic.Toggled += Stylechanged;
			adjustHBox.PackStart (togglebuttonBold);
			adjustHBox.PackStart (togglebuttonItalic);
			adjustHBox.PackStart (buttonFormat);
			table.Add (adjustHBox, 2, 1);

			textEditor = new TextEditor ();
			textEditor.Options = DefaultSourceEditorOptions.Instance;
			textEditor.ShowAll ();
			Toolkit toolkit = Toolkit.CurrentEngine;
			var wrappedTextEditor = toolkit.WrapWidget (textEditor);
			table.Add (wrappedTextEditor, 2, 2);

			mainTable.Add (table, 0, 1, 1, 1, true, true);
			this.Content = mainTable;

			HandleTreeviewColorsSelectionChanged (null, null);
		}

		void ApplyStyle (ColorScheme sheme)
		{
			sheme.Name = entryName.Text;
			sheme.Description = entryDescription.Text;

			TreePosition iter = treeviewColors.SelectedRow;
			if (iter == null)
				return;

			var navigator = colorStore.GetFirstNode ();

			do {
				var data = (ColorScheme.PropertyDecsription)navigator.GetValue (propertyField);
				var style = navigator.GetValue (styleField);
				data.Info.SetValue (sheme, style, null);
			} while (navigator.MoveNext());			
		}

		public static void RefreshAllColors ()
		{
			foreach (var doc in Ide.IdeApp.Workbench.Documents) {
				var editor = doc.Editor;
				if (editor == null)
					continue;
				doc.UpdateParseDocument ();
				editor.Parent.TextViewMargin.PurgeLayoutCache ();
				editor.Document.CommitUpdateAll ();
			}
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd.Equals (Command.Ok)) {
				ApplyStyle (colorSheme);
				try {
					if (fileName.EndsWith (".vssettings", StringComparison.Ordinal)) {
						System.IO.File.Delete (fileName);
						fileName += "Style.json";
					}
					colorSheme.Save (fileName);
					panel.ShowStyles ();
				} catch (Exception ex) {
					MessageService.ShowException (ex);
				}
				RefreshAllColors ();
			}
			base.OnCommandActivated (cmd);
		}

		void HandleTreeviewColorsSelectionChanged (object sender, EventArgs e)
		{
			this.colorbuttonPrimary.Sensitive = false;
			this.colorbuttonSecondary.Sensitive = false;
			this.colorbuttonBorder.Sensitive = false;
			this.togglebuttonBold.Sensitive = false;
			this.togglebuttonItalic.Sensitive = false;


			TreePosition pos = treeviewColors.SelectedRow;
			if (pos == null)
				return;

			var navigator = colorStore.GetNavigatorAt (pos);
			var o = navigator.GetValue (styleField);

			if (o is ChunkStyle)
				SelectChunkStyle ((ChunkStyle)o);

			if (o is AmbientColor)
				SelectAmbientColor ((AmbientColor)o);
		}

		object GetSelectedStyle ()
		{
			TreePosition pos = treeviewColors.SelectedRow;
			if (pos == null)
				return null;

			var navigator = colorStore.GetNavigatorAt (pos);
			return navigator.GetValue (styleField);
		}

		void Stylechanged (object sender, EventArgs e)
		{
			TreePosition pos = treeviewColors.SelectedRow;
			if (pos == null)
				return;

			var navigator = colorStore.GetNavigatorAt (pos);
			var o = navigator.GetValue (styleField);

			if (o is ChunkStyle) {
				SetChunkStyle (navigator, (ChunkStyle)o);
			} else if (o is AmbientColor) {
				SetAmbientColor (navigator, (AmbientColor)o);
			}
		}

		Cairo.Color GetColorFromButton (LabeledColorButton button)
		{
			return new Cairo.Color (button.Color.Red, button.Color.Green, button.Color.Blue, button.Color.Alpha);
		}

		void SetAmbientColor (TreeNavigator navigator, AmbientColor oldStyle)
		{
			var newStyle = new AmbientColor ();
			newStyle.Color = GetColorFromButton (colorbuttonPrimary);
			newStyle.SecondColor = GetColorFromButton (colorbuttonSecondary);

			navigator.SetValue (styleField, newStyle);

			var newscheme = colorSheme.Clone ();
			ApplyStyle (newscheme);

			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newscheme;
			this.textEditor.QueueDraw ();
		}

		void SetChunkStyle (TreeNavigator navigator, ChunkStyle oldStyle)
		{
			var newStyle = new ChunkStyle (oldStyle);
			newStyle.Foreground = GetColorFromButton (colorbuttonPrimary);
			newStyle.Background = GetColorFromButton (colorbuttonSecondary);

			if (togglebuttonBold.Active) {
				newStyle.FontWeight = Xwt.Drawing.FontWeight.Bold;
			} else {
				newStyle.FontWeight = Xwt.Drawing.FontWeight.Normal;
			}

			if (togglebuttonItalic.Active) {
				newStyle.FontStyle = Xwt.Drawing.FontStyle.Italic;
			} else {
				newStyle.FontStyle = Xwt.Drawing.FontStyle.Normal;
			}

			navigator.SetValue (styleField, newStyle);

			var newscheme = colorSheme.Clone ();
			ApplyStyle (newscheme);

			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newscheme;
			this.textEditor.QueueDraw ();
		}

		void SetColorToButton (LabeledColorButton button, Cairo.Color color)
		{
			button.Color = new Color (color.R, color.G, color.B, color.A);
		}

		void SelectAmbientColor (AmbientColor ambientColor)
		{
			SetColorToButton (colorbuttonPrimary, ambientColor.Color);
			SetColorToButton (colorbuttonSecondary, ambientColor.SecondColor);
			colorbuttonSecondary.Sensitive = ambientColor.HasSecondColor;
			SetColorToButton (colorbuttonBorder, ambientColor.BorderColor);
			colorbuttonBorder.Visible = true;
			colorbuttonBorder.Sensitive = ambientColor.HasBorderColor;
			togglebuttonBold.Visible = false;
			togglebuttonItalic.Visible = false;

			colorbuttonPrimary.LabelText = "Primary color:";
			colorbuttonSecondary.LabelText = "Secondary color:";
		}

		void SelectChunkStyle (ChunkStyle chunkStyle)
		{
			SetColorToButton (colorbuttonPrimary, chunkStyle.Foreground);
			SetColorToButton (colorbuttonSecondary, chunkStyle.Background);

			togglebuttonBold.Active = chunkStyle.FontWeight == Xwt.Drawing.FontWeight.Bold;
			togglebuttonItalic.Active = chunkStyle.FontStyle == Xwt.Drawing.FontStyle.Italic;

			this.colorbuttonPrimary.LabelText = "Foreground:";
			this.colorbuttonSecondary.LabelText = "Background:";
			this.colorbuttonPrimary.Sensitive = true;
			this.colorbuttonSecondary.Sensitive = true;
			this.togglebuttonBold.Visible = true;
			this.togglebuttonBold.Sensitive = true;
			this.togglebuttonItalic.Visible = true;
			this.togglebuttonItalic.Sensitive = true;

			colorbuttonBorder.Visible = false;
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
				var navigator = colorStore.AddNode ();
				navigator.SetValue (nameField, data.Attribute.Name);
				navigator.SetValue (propertyField, data);
				navigator.SetValue (styleField, data.Info.GetValue (style, null));
			}

			foreach (var data in ColorScheme.AmbientColors) {
				var navigator = colorStore.AddNode ();
				navigator.SetValue (nameField, data.Attribute.Name);
				navigator.SetValue (propertyField, data);
				navigator.SetValue (styleField, data.Info.GetValue (style, null));
			}

			Stylechanged (null, null);
		}
	}
}
