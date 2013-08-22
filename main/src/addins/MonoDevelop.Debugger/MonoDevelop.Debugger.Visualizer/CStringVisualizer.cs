//
// CStringVisualizer.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Jeffrey Stedfast
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
using System.Collections.Generic;

using Gtk;

using Mono.Debugging.Client;
using MonoDevelop.Core;

namespace MonoDevelop.Debugger.Visualizer
{
	public class CStringVisualizer : ValueVisualizer
	{
		TextView textView;

		public CStringVisualizer ()
		{
		}

		public override string Name {
			get { return GettextCatalog.GetString ("C String"); }
		}

		public override bool CanVisualize (ObjectValue val)
		{
			switch (val.TypeName) {
			case "sbyte[]": return true;
			case "byte[]": return true;
			default: return false;
			}
		}

		static void AppendByte (StringBuilder text, byte c)
		{
			switch (c) {
			case 0x00: text.Append ("\\0"); break;
			case 0x07: text.Append ("\\a"); break;
			case 0x08: text.Append ("\\b"); break;
			case 0x09: text.Append ("\\t"); break;
			case 0x0A: text.Append ("\\n"); break;
			case 0x0B: text.Append ("\\v"); break;
			case 0x0D: text.Append ("\\r"); break;
			default:
				if (c < 020 || c > 0x7e) {
					text.AppendFormat ("\\x{0:x,2}", c);
				} else {
					text.Append ((char) c);
				}
				break;
			}
		}

		static string ByteArrayToCString (byte[] buf)
		{
			StringBuilder text = new StringBuilder ();

			for (int i = 0; i < buf.Length; i++)
				AppendByte (text, buf[i]);

			return text.ToString ();
		}

		static string SByteArrayToCString (sbyte[] buf)
		{
			StringBuilder text = new StringBuilder ();

			for (int i = 0; i < buf.Length; i++)
				AppendByte (text, (byte) buf[i]);

			return text.ToString ();
		}

		void PopulateTextView (ObjectValue value)
		{
			var array = value.GetRawValue () as RawValueArray;
			var iter = textView.Buffer.EndIter;
			string text;

			switch (value.TypeName) {
			case "sbyte[]":
				text = SByteArrayToCString (array.ToArray () as sbyte[]);
				break;
			case "byte[]":
				text = ByteArrayToCString (array.ToArray () as byte[]);
				break;
			default:
				text = string.Empty;
				break;
			}

			textView.Buffer.Insert (ref iter, text);
		}

		public override Widget GetVisualizerWidget (ObjectValue val)
		{
			textView = new TextView () { WrapMode = WrapMode.Char };

			var scrolled = new ScrolledWindow () {
				HscrollbarPolicy = PolicyType.Automatic,
				VscrollbarPolicy = PolicyType.Automatic,
				ShadowType = ShadowType.In
			};
			scrolled.Add (textView);

			var check = new CheckButton (GettextCatalog.GetString ("Wrap text"));
			check.Active = true;
			check.Toggled += delegate {
				if (check.Active)
					textView.WrapMode = WrapMode.WordChar;
				else
					textView.WrapMode = WrapMode.None;
			};

			var box = new VBox (false, 6);
			box.PackStart (scrolled, true, true, 0);
			box.PackStart (check, false, false, 0);
			box.ShowAll ();

			PopulateTextView (val);

			return box;
		}
	}
}
