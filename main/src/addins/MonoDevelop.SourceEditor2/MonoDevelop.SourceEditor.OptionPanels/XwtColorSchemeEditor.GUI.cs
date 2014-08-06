//
// XwtColorSchemeEditor.GUI.cs
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
using MonoDevelop.Ide.Gui;
using System.Linq;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class XwtColorSchemeEditor
	{
		bool formatByPatternMode;
		bool handleUIEvents = true;
		ColorSchemeEditorHistory history;
		TextEditor textEditor;
		ColorScheme colorScheme;
		string fileName;
		HighlightingPanel panel;
		DataField<string> nameField = new DataField<string> ();
		DataField<object> styleField = new DataField<object> ();
		DataField<ColorScheme.PropertyDecsription> propertyField = new DataField<ColorScheme.PropertyDecsription> ();
		TreeStore colorStore;
		TreeView treeviewColors = new TreeView ();
		LabeledColorButton colorbuttonPrimary = new LabeledColorButton ("Primary");
		LabeledColorButton colorbuttonSecondary = new LabeledColorButton ("Secondary");
		LabeledColorButton colorbuttonBorder = new LabeledColorButton ("Border");
		ToggleButton togglebuttonBold = new ToggleButton (ImageService.GetIcon (Stock.BoldIcon).WithSize (Xwt.IconSize.Medium)){ Style = ButtonStyle.Flat };
		ToggleButton togglebuttonItalic = new ToggleButton (ImageService.GetIcon (Stock.ItalicIcon).WithSize (Xwt.IconSize.Medium)){ Style = ButtonStyle.Flat };
		ToggleButton buttonFormat = new ToggleButton (ImageService.GetIcon (Stock.ColorPickerIcon).WithSize (Xwt.IconSize.Medium)){ Style = ButtonStyle.Flat };
		TextEntry entryName = new TextEntry ();
		TextEntry entryDescription = new TextEntry ();
		SearchTextEntry searchEntry = new SearchTextEntry (){ PlaceholderText = "Type here..." };
		Button undoButton = new Button (ImageService.GetIcon (Stock.UndoIcon).WithSize (Xwt.IconSize.Medium)) {
			Sensitive = false,
			Style = ButtonStyle.Flat
		};
		Button redoButton = new Button (ImageService.GetIcon (Stock.RedoIcon).WithSize (Xwt.IconSize.Medium)) {
			Sensitive = false,
			Style = ButtonStyle.Flat
		};

		private void Build ()
		{
			this.Buttons.Add (new DialogButton (Command.Cancel));
			this.Buttons.Add (new DialogButton (Command.Ok));

			colorStore = CreateColorStore ();

			var mainTable = new Table ();

			var headerTable = new Table ();
			headerTable.Add (new Label () { Text = "Name:" }, 0, 0);
			headerTable.Add (entryName, 1, 0);
			headerTable.Add (new Label () { Text = "Description:" }, 2, 0);
			headerTable.Add (entryDescription, 3, 0, 1, 1, true);
			mainTable.Add (headerTable, 0, 0, 1, 1, true, false, WidgetPlacement.Fill, WidgetPlacement.Start);

			var table = new Table ();

			var commandHBox = new HBox ();
			var undoRedoButton = new SegmentedButton ();
			undoRedoButton.Items.Add (undoButton);
			undoRedoButton.Items.Add (redoButton);
			undoButton.Clicked += Undo;
			redoButton.Clicked += Redo;
			commandHBox.PackStart (undoRedoButton);
			commandHBox.PackStart (new Button ("AutoSet"){ Style = ButtonStyle.Flat });
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
			this.buttonFormat.Toggled += FormatByPatternToggled;

			this.textEditor = new TextEditor ();
			this.textEditor.Options = DefaultSourceEditorOptions.Instance;
			this.textEditor.ShowAll ();
			var toolkit = Toolkit.CurrentEngine;
			var wrappedTextEditor = toolkit.WrapWidget (textEditor);
			var scrollView = new ScrollView (wrappedTextEditor) {
				HorizontalScrollPolicy = ScrollPolicy.Always,
				VerticalScrollPolicy = ScrollPolicy.Always
			};
			table.Add (scrollView, 0, 2, 1, 1, true, true);

			this.treeviewColors = new TreeView ();
			this.treeviewColors.Columns.Add (GettextCatalog.GetString ("Name"), nameField);
			this.treeviewColors.HeadersVisible = false;
			this.treeviewColors.DataSource = colorStore;
			this.treeviewColors.SelectionChanged += TreeviewColorsSelectionChanged;
			history = new ColorSchemeEditorHistory (treeviewColors, styleField);
			history.CanUndoRedoChanged += CanUndoRedoChanged;

			var box = new HPaned ();

			var treeBox = new VBox ();
			treeBox.PackStart (searchEntry);
			searchEntry.MarginTop = 1;
			treeBox.PackStart (treeviewColors, true, true);
			box.Panel1.Content = treeBox;
			box.Panel2.Content = table;
			table.MarginLeft = 3;
			box.Panel2.Resize = true;
			box.Position = 300;
			
			mainTable.Add (box, 0, 1, 1, 1, true, true);
			this.Content = mainTable;

			searchEntry.Changed += SearchTextChanged;

			this.Height = 500;
			this.Width = 800;
		}

		TreeStore CreateColorStore ()
		{
			return new TreeStore (nameField, styleField, propertyField);
		}
	}
}

