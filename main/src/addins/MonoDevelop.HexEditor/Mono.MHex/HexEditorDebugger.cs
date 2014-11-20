//
// HexEditorDebugger.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using Mono.MHex.Data;
using Xwt;

namespace Mono.MHex
{
	class HexEditorDebugger : VBox
	{
		public HexEditorData HexEditorData {
			get {
				return editor.HexEditorData;
			}
			set {
				editor.HexEditorData = value;
			}
		}

		public IHexEditorOptions Options {
			get {
				return editor.Options;
			}
			set {
				editor.Options = value;
			}
		}

		public void PurgeLayoutCaches ()
		{
			editor.PurgeLayoutCaches ();
		}

		public HexEditor Editor {
			get {
				return editor;
			}
		}

		HexEditor editor = new HexEditor ();

		public void Repaint ()
		{
			editor.Repaint ();
		}

		public HexEditorDebugger ()
		{
			var comboBox = new ComboBox ();
			comboBox.Items.Add ("Hex 8");
			comboBox.Items.Add ("Hex 16");
			comboBox.SelectedIndex = 0;
			editor.Options.StringRepresentationType = StringRepresentationTypes.ASCII;
			comboBox.SelectionChanged += delegate {
				switch (comboBox.SelectedIndex) {
				case 0:
					editor.Options.StringRepresentationType = StringRepresentationTypes.ASCII;
					break;
				case 1:
					editor.Options.StringRepresentationType = StringRepresentationTypes.UTF16;
					break;
				}
			};
			comboBox.HorizontalPlacement = WidgetPlacement.End;
			Spacing = 0;
			PackStart (comboBox);
			PackStart (new ScrollView (editor), true);
		}
	}
}

