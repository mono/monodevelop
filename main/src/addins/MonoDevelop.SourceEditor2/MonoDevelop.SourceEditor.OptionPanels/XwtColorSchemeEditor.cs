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
		ColorScheme colorScheme;
		string fileName;
		HighlightingPanel panel;
		DataField<string> nameField = new DataField<string> ();
		DataField<object> styleField = new DataField<object> ();
		DataField<ColorScheme.PropertyDecsription> propertyField = new DataField<ColorScheme.PropertyDecsription> ();
		TreeStore colorStore;
		TreeView treeviewColors = new TreeView ();
		LabeledColorButton colorbuttonPrimary = new LabeledColorButton ("Primary color:");
		LabeledColorButton colorbuttonSecondary = new LabeledColorButton ("Secondary color:");
		LabeledColorButton colorbuttonBorder = new LabeledColorButton ("Border color:");
		ToggleButton togglebuttonBold = new ToggleButton ("B");
		ToggleButton togglebuttonItalic = new ToggleButton ("I");
		Button buttonFormat = new Button ("FBP");
		TextEntry entryName = new TextEntry ();
		TextEntry entryDescription = new TextEntry ();
		TextEntry searchEntry = new TextEntry ();

		public XwtColorSchemeEditor (HighlightingPanel panel)
		{
			this.panel = panel;

			this.Buttons.Add (new DialogButton (Command.Cancel));
			this.Buttons.Add (new DialogButton (Command.Ok));

			colorStore = new TreeStore (nameField, styleField, propertyField);

			var mainTable = new Table ();

			var headerTable = new Table ();
			headerTable.Add (new Label () { Text="Name:" }, 0, 0);
			headerTable.Add (entryName, 1, 0);
			headerTable.Add (new Label () { Text="Description:" }, 2, 0);
			headerTable.Add (entryDescription, 3, 0, 1, 1, true);
			mainTable.Add (headerTable, 0, 0, 1, 1, true, false, WidgetPlacement.Fill, WidgetPlacement.Start);

			var table = new Table ();

			var commandHBox = new HBox ();
			commandHBox.PackStart (new Button ("Undo"));
			commandHBox.PackStart (new Button ("Redo"));
			commandHBox.PackStart (new Button ("AutoSet"));
			table.Add (commandHBox, 0, 0);

			var adjustHBox = new HBox ();
			adjustHBox.PackStart (colorbuttonPrimary);
			adjustHBox.PackStart (colorbuttonSecondary);
			adjustHBox.PackStart (colorbuttonBorder);
			adjustHBox.PackStart (togglebuttonBold, false, WidgetPlacement.End);
			adjustHBox.PackStart (togglebuttonItalic, false, WidgetPlacement.End);
			adjustHBox.PackStart (buttonFormat, false, WidgetPlacement.End);
			table.Add (adjustHBox, 0, 1);

			this.colorbuttonPrimary.ColorSet += StyleChanged;
			this.colorbuttonSecondary.ColorSet += StyleChanged;
			this.colorbuttonBorder.ColorSet += StyleChanged;
			this.togglebuttonBold.Toggled += StyleChanged;
			this.togglebuttonItalic.Toggled += StyleChanged;

			this.textEditor = new TextEditor ();
			this.textEditor.Options = DefaultSourceEditorOptions.Instance;
			this.textEditor.ShowAll ();
			var toolkit = Toolkit.CurrentEngine;
			var wrappedTextEditor = toolkit.WrapWidget (textEditor);
			var scrollView = new ScrollView (wrappedTextEditor) {
				HorizontalScrollPolicy=ScrollPolicy.Always,
				VerticalScrollPolicy=ScrollPolicy.Always
			};
			table.Add (scrollView, 0, 2, 1, 1, true, true);

			this.treeviewColors = new TreeView ();
			this.treeviewColors.Columns.Add (GettextCatalog.GetString ("Name"), nameField);
			this.treeviewColors.HeadersVisible = false;
			this.treeviewColors.DataSource = colorStore;
			this.treeviewColors.SelectionChanged += HandleTreeviewColorsSelectionChanged;

			var box = new HPaned ();

			var treeBox = new VBox ();
			treeBox.PackStart (searchEntry);
			treeBox.PackStart (treeviewColors, true);
			box.Panel1.Content = treeBox;
			box.Panel2.Content = table;
			box.Panel2.Resize = true;
			box.Position = 400;
			
			mainTable.Add (box, 0, 1, 1, 1, true, true);
			this.Content = mainTable;

			this.Height = 400;

			HandleTreeviewColorsSelectionChanged (null, null);
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

		void SelectChunkStyle (ChunkStyle chunkStyle)
		{
			SetColorToButton (colorbuttonPrimary, chunkStyle.Foreground);
			SetColorToButton (colorbuttonSecondary, chunkStyle.Background);

			this.togglebuttonBold.Active = chunkStyle.FontWeight == Xwt.Drawing.FontWeight.Bold;
			this.togglebuttonItalic.Active = chunkStyle.FontStyle == Xwt.Drawing.FontStyle.Italic;

			this.colorbuttonPrimary.LabelText = "Foreground:";
			this.colorbuttonSecondary.LabelText = "Background:";

			this.colorbuttonPrimary.Sensitive = true;
			this.colorbuttonSecondary.Sensitive = true;
			this.togglebuttonBold.Sensitive = true;
			this.togglebuttonItalic.Sensitive = true;

			this.togglebuttonBold.Visible = true;
			this.togglebuttonItalic.Visible = true;
			this.colorbuttonBorder.Visible = false;
		}

		void SelectAmbientColor (AmbientColor ambientColor)
		{
			SetColorToButton (colorbuttonPrimary, ambientColor.Color);
			SetColorToButton (colorbuttonSecondary, ambientColor.SecondColor);
			SetColorToButton (colorbuttonBorder, ambientColor.BorderColor);

			this.colorbuttonPrimary.Sensitive = true;
			this.colorbuttonSecondary.Sensitive = ambientColor.HasSecondColor;
			this.colorbuttonBorder.Sensitive = ambientColor.HasBorderColor;

			this.colorbuttonBorder.Visible = true;
			this.togglebuttonBold.Visible = false;
			this.togglebuttonItalic.Visible = false;

			this.colorbuttonPrimary.LabelText = "Primary color:";
			this.colorbuttonSecondary.LabelText = "Secondary color:";
		}

		void SetColorToButton (LabeledColorButton button, Cairo.Color color)
		{
			button.Color = new Color (color.R, color.G, color.B, color.A);
		}

		void StyleChanged (object sender, EventArgs e)
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

		void SetChunkStyle (TreeNavigator navigator, ChunkStyle oldStyle)
		{
			var newStyle = new ChunkStyle (oldStyle);
			newStyle.Foreground = GetColorFromButton (colorbuttonPrimary);
			newStyle.Background = GetColorFromButton (colorbuttonSecondary);

			if (togglebuttonBold.Active) {
				newStyle.FontWeight = FontWeight.Bold;
			} else {
				newStyle.FontWeight = FontWeight.Normal;
			}

			if (togglebuttonItalic.Active) {
				newStyle.FontStyle = FontStyle.Italic;
			} else {
				newStyle.FontStyle = FontStyle.Normal;
			}

			navigator.SetValue (styleField, newStyle);

			var newscheme = colorScheme.Clone ();
			ApplyStyle (newscheme);

			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newscheme;
			this.textEditor.QueueDraw ();
		}

		void SetAmbientColor (TreeNavigator navigator, AmbientColor oldStyle)
		{
			var newStyle = new AmbientColor ();
			newStyle.Color = GetColorFromButton (colorbuttonPrimary);
			newStyle.SecondColor = GetColorFromButton (colorbuttonSecondary);

			navigator.SetValue (styleField, newStyle);

			var newscheme = colorScheme.Clone ();
			ApplyStyle (newscheme);

			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newscheme;
			this.textEditor.QueueDraw ();
		}

		Cairo.Color GetColorFromButton (LabeledColorButton button)
		{
			return new Cairo.Color (button.Color.Red, button.Color.Green, button.Color.Blue, button.Color.Alpha);
		}

		void ApplyStyle (ColorScheme scheme)
		{
			scheme.Name = entryName.Text;
			scheme.Description = entryDescription.Text;

			TreePosition iter = treeviewColors.SelectedRow;
			if (iter == null)
				return;

			var navigator = colorStore.GetFirstNode ();

			do {
				var data = (ColorScheme.PropertyDecsription)navigator.GetValue (propertyField);
				var style = navigator.GetValue (styleField);
				data.Info.SetValue (scheme, style, null);
			} while (navigator.MoveNext());			
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd.Equals (Command.Ok)) {
				ApplyStyle (colorScheme);
				try {
					if (fileName.EndsWith (".vssettings", StringComparison.Ordinal)) {
						System.IO.File.Delete (fileName);
						fileName += "Style.json";
					}
					colorScheme.Save (fileName);
					panel.ShowStyles ();
				} catch (Exception ex) {
					MessageService.ShowException (ex);
				}
				RefreshAllColors ();
			}
			base.OnCommandActivated (cmd);
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

		public void SetScheme (ColorScheme scheme)
		{
			if (scheme == null)
				throw new ArgumentNullException ("scheme");

			this.fileName = scheme.FileName;
			this.colorScheme = scheme;
			this.entryName.Text = scheme.Name;
			this.entryDescription.Text = scheme.Description;
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = scheme;
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
				navigator.SetValue (styleField, data.Info.GetValue (scheme, null));
			}

			foreach (var data in ColorScheme.AmbientColors) {
				var navigator = colorStore.AddNode ();
				navigator.SetValue (nameField, data.Attribute.Name);
				navigator.SetValue (propertyField, data);
				navigator.SetValue (styleField, data.Info.GetValue (scheme, null));
			}

			StyleChanged (null, null);
		}
	}
}
