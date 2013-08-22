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
using System.Text;

using Gtk;

using Mono.Debugging.Client;
using MonoDevelop.Debugger;
using MonoDevelop.Core;

using Mono.MHex.Data;

namespace MonoDevelop.HexEditor
{
	public class HexEditorVisualizer : ValueVisualizer
	{
		Mono.MHex.HexEditor hexEditor;

		public HexEditorVisualizer ()
		{
		}

		#region IValueVisualizer implementation

		public override string Name {
			get { return GettextCatalog.GetString ("HexEdit"); }
		}

		public override bool CanVisualize (ObjectValue val)
		{
			switch (val.TypeName) {
			case "sbyte[]": return true;
			case "byte[]": return true;
			case "char[]": return true;
			case "string": return true;
			default: return false;
			}
		}

		public override bool IsDefaultVisualizer (ObjectValue val)
		{
			switch (val.TypeName) {
				case "sbyte[]":
				case "byte[]": return true;
				case "char[]": 
				case "string": return false;
				default: return false;
			}
		}

		void SetHexEditorOptions ()
		{
			hexEditor.Options.FontName = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.FontName;
			hexEditor.PurgeLayoutCaches ();
			hexEditor.Repaint ();
		}

		public override Widget GetVisualizerWidget (ObjectValue val)
		{
			hexEditor = new Mono.MHex.HexEditor ();

			byte[] buf = null;

			if (val.TypeName != "string") {
				var raw = val.GetRawValue () as RawValueArray;
				sbyte[] sbuf;

				switch (val.TypeName) {
				case "sbyte[]":
					sbuf = raw.ToArray () as sbyte[];
					buf = new byte[sbuf.Length];
					for (int i = 0; i < sbuf.Length; i++)
						buf[i] = (byte) sbuf[i];
					break;
				case "char[]":
					buf = Encoding.Unicode.GetBytes (new string (raw.ToArray () as char[]));
					break;
				case "byte[]":
					buf = raw.ToArray () as byte[];
					break;
				}
			} else {
				var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
				ops.ChunkRawStrings = true;

				var raw = val.GetRawValue (ops) as RawValueString;

				buf = Encoding.Unicode.GetBytes (raw.Value);
			}

			hexEditor.HexEditorData.Buffer = new ArrayBuffer (buf);
			hexEditor.Sensitive = CanEdit (val);

			var xwtScrollView = new Xwt.ScrollView (hexEditor);
			var scrollWidget = (Widget) Xwt.Toolkit.CurrentEngine.GetNativeWidget (xwtScrollView);
			SetHexEditorOptions ();
			hexEditor.SetFocus ();
			return scrollWidget;
		}

		public override bool StoreValue (ObjectValue val)
		{
			switch (val.TypeName) {
			case "byte[]":
				val.SetRawValue (hexEditor.HexEditorData.Bytes);
				return true;
			default:
				return false;
			}
		}

		public override bool CanEdit (ObjectValue val)
		{
			switch (val.TypeName) {
			case "byte[]": return true;
			default: return false;
			}
		}

		#endregion
	}
}
