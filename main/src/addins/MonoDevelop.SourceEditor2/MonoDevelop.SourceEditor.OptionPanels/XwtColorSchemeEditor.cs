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

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class XwtColorSchemeEditor:Dialog
	{
		class LabeledColorPicker:VBox
		{
			Label label = new Label ();
			ColorSelector colorSelector = new ColorSelector ();

			public LabeledColorPicker ()
			{
				PackStart (label);
				PackStart (colorSelector);
			}

			public LabeledColorPicker (string label)
				:this()
			{
				this.label.Text = label;
			}
		}

		public XwtColorSchemeEditor ()
		{
			this.Buttons.Add (new DialogButton (Command.Cancel));
			this.Buttons.Add (new DialogButton (Command.Ok));

			var mainTable = new Table ();

			var schemeTable = new Table ();
			schemeTable.Add (new Label () { Text="Name:" }, 0, 0);
			schemeTable.Add (new TextEntry (), 1, 0);
			schemeTable.Add (new Label () { Text="Description:" }, 2, 0);
			schemeTable.Add (new TextEntry (), 3, 0);

			mainTable.Add (schemeTable, 0, 0);

			var table = new Table ();

			table.Add (new TextEntry (), 0, 0);
			table.Add (new TreeView (), 0, 1, 2, 1, true, true);

			table.Add (new VSeparator (), 1, 0, 3);

			var commandHBox = new HBox ();
			commandHBox.PackStart (new Button ("Undo"));
			commandHBox.PackStart (new Button ("Redo"));
			commandHBox.PackStart (new Button ("AutoSet"));
			table.Add (commandHBox, 2, 0);

			var adjustHBox = new HBox ();
			adjustHBox.PackStart (new LabeledColorPicker ("Color one"));
			adjustHBox.PackStart (new LabeledColorPicker ("Color two"));
			adjustHBox.PackStart (new ToggleButton ("Bold"));
			adjustHBox.PackStart (new ToggleButton ("Italic"));
			adjustHBox.PackStart (new Button ("FormatByPattern"));
			table.Add (adjustHBox, 2, 1);

			var textEditor = new TextEditor ();
			Toolkit toolkit = Toolkit.CurrentEngine;
			var wrappedTextEditor = toolkit.WrapWidget (textEditor);
			table.Add (wrappedTextEditor, 2, 2);

			mainTable.Add (table, 0, 1, 1, 1, true, true);
			this.Content = mainTable;
		}
	}
}
