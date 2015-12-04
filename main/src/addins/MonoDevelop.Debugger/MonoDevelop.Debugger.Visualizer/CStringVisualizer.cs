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
		const int CHUNK_SIZE = 1024;

		RawValueArray rawArray;
		TextView textView;
		uint idle_id;
		int length;
		int offset;

		public CStringVisualizer ()
		{
		}

		public override string Name {
			get { return GettextCatalog.GetString ("C String"); }
		}

		public override bool CanVisualize (ObjectValue val)
		{
			switch (val.TypeName) {
			case "System.IO.MemoryStream":
			case "Foundation.NSData":
			case "sbyte[]":
			case "byte[]":
				return true;
			default:
				return false;
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
					text.AppendFormat ("\\x{0:x2}", c);
				} else {
					text.Append ((char) c);
				}
				break;
			}
		}

		static string ByteArrayToCString (byte[] buf)
		{
			var text = new StringBuilder ();

			for (int i = 0; i < buf.Length; i++)
				AppendByte (text, buf[i]);

			return text.ToString ();
		}

		static string SByteArrayToCString (sbyte[] buf)
		{
			var text = new StringBuilder ();

			for (int i = 0; i < buf.Length; i++)
				AppendByte (text, (byte) buf[i]);

			return text.ToString ();
		}

		bool GetNextSByteArrayChunk ()
		{
			int amount = Math.Min (length - offset, CHUNK_SIZE);
			var chunk = rawArray.GetValues (offset, amount) as sbyte[];
			var text = SByteArrayToCString (chunk);
			var iter = textView.Buffer.EndIter;

			textView.Buffer.Insert (ref iter, text);
			offset += amount;

			if (offset < length)
				return true;

			idle_id = 0;

			// Remove this idle callback
			return false;
		}

		bool GetNextByteArrayChunk ()
		{
			int amount = Math.Min (length - offset, CHUNK_SIZE);
			var chunk = rawArray.GetValues (offset, amount) as byte[];
			var text = ByteArrayToCString (chunk);
			var iter = textView.Buffer.EndIter;

			textView.Buffer.Insert (ref iter, text);
			offset += amount;

			if (offset < length)
				return true;

			idle_id = 0;

			// Remove this idle callback
			return false;
		}

		void PopulateTextView (ObjectValue value)
		{
			var session = DebuggingService.DebuggerSession;
			var ops = session.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;

			switch (value.TypeName) {
			case "MonoTouch.Foundation.NSData":
			case "MonoMac.Foundation.NSData":
			case "System.IO.MemoryStream":
			case "Foundation.NSData":
				var stream = value.GetRawValue (ops) as RawValue;
				rawArray = stream.CallMethod ("ToArray") as RawValueArray;
				break;
			default:
				rawArray = value.GetRawValue (ops) as RawValueArray;
				break;
			}

			length = rawArray.Length;
			offset = 0;

			if (length > 0) {
				switch (value.TypeName) {
				case "sbyte[]":
					idle_id = GLib.Idle.Add (GetNextSByteArrayChunk);
					break;
				case "MonoTouch.Foundation.NSData":
				case "MonoMac.Foundation.NSData":
				case "System.IO.MemoryStream":
				case "Foundation.NSData":
				case "byte[]":
					idle_id = GLib.Idle.Add (GetNextByteArrayChunk);
					break;
				default:
					return;
				}

				textView.Destroyed += delegate {
					if (idle_id != 0) {
						GLib.Source.Remove (idle_id);
						idle_id = 0;
					}
				};
			}
		}

		public override Widget GetVisualizerWidget (ObjectValue val)
		{
			textView = new TextView { WrapMode = WrapMode.Char };

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
