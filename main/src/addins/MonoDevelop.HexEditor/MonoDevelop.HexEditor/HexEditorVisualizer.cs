//
// HexEditorVisualizer.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using Gtk;

using Mono.Debugging.Client;
using MonoDevelop.Debugger;
using MonoDevelop.Core;

using Mono.MHex.Data;

namespace MonoDevelop.HexEditor
{
	public class HexEditorVisualizer : IValueVisualizer
	{
		Mono.MHex.HexEditor hexEditor;

		public HexEditorVisualizer ()
		{
		}

		#region IValueVisualizer implementation

		public string Name {
			get { return GettextCatalog.GetString ("HexEditor"); }
		}

		public bool CanVisualize (ObjectValue val)
		{
			switch (val.TypeName) {
			case "byte[]": return true;
			default: return false;
			}
		}

		void SetHexEditorOptions ()
		{
			hexEditor.Options.FontName = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.FontName;
			hexEditor.PurgeLayoutCaches ();
			hexEditor.Repaint ();
		}

		public Widget GetVisualizerWidget (ObjectValue val)
		{
			hexEditor = new Mono.MHex.HexEditor ();

			var raw = val.GetRawValue () as RawValueArray;
			var buf = raw.ToArray () as byte[];

			var buffer = new byte[buf.LongLength];
			Array.Copy (buf, buffer, buf.Length);

			hexEditor.HexEditorData.Buffer = new ArrayBuffer (buffer);

			var scrolled = new ScrolledWindow () {
				HscrollbarPolicy = PolicyType.Automatic,
				VscrollbarPolicy = PolicyType.Automatic,
				ShadowType = ShadowType.In
			};

			var hexEditorWidget = (Widget) Xwt.Toolkit.CurrentEngine.GetNativeWidget (hexEditor);
			scrolled.AddWithViewport (hexEditorWidget);
			scrolled.ShowAll ();

			SetHexEditorOptions ();
			hexEditor.SetFocus ();

			return scrolled;
		}

		public bool StoreValue (ObjectValue val)
		{
			val.SetRawValue (hexEditor.HexEditorData.Bytes);
			return true;
		}

		public bool CanEdit (ObjectValue val)
		{
			return true;
		}

		#endregion
	}
}
