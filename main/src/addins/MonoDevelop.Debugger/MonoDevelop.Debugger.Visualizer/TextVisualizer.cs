// 
// TextVisualizer.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Debugger.Visualizer
{
	public class TextVisualizer: IValueVisualizer
	{
		const int CHUNK_SIZE = 1024;
		
		TextView textView;
		RawValueString raw;
		ObjectValue value;
		uint idle_id;
		int length;
		int offset;
		
		public TextVisualizer ()
		{
		}
		
		public string Name {
			get { return GettextCatalog.GetString ("Text"); }
		}
		
		public bool CanVisualize (ObjectValue val)
		{
			switch (val.TypeName) {
			case "sbyte[]": return true;
			case "byte[]": return true;
			case "char[]": return true;
			case "string": return true;
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
		
		bool GetNextStringChunk ()
		{
			int amount = Math.Min (length - offset, CHUNK_SIZE);
			string chunk = raw.Substring (offset, amount);
			TextIter iter = textView.Buffer.EndIter;
			
			textView.Buffer.Insert (ref iter, chunk);
			offset += amount;
			
			if (offset < length)
				return true;
			
			idle_id = 0;
			
			// Remove this idle callback
			return false;
		}

		void PopulateTextView ()
		{
			if (value.TypeName == "string") {
				EvaluationOptions ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
				ops.ChunkRawStrings = true;

				raw = value.GetRawValue (ops) as RawValueString;
				length = raw.Length;
				offset = 0;

				if (length > 0) {
					idle_id = GLib.Idle.Add (GetNextStringChunk);
					textView.Destroyed += delegate {
						if (idle_id != 0) {
							GLib.Source.Remove (idle_id);
							idle_id = 0;
						}
					};
				}
			} else {
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
				case "char[]":
					text = new string (array.ToArray () as char[]);
					break;
				default:
					text = string.Empty;
					break;
				}

				textView.Buffer.Insert (ref iter, text);
			}
		}

		public Widget GetVisualizerWidget (ObjectValue val)
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
					textView.WrapMode = WrapMode.Char;
				else
					textView.WrapMode = WrapMode.None;
			};

			var box = new VBox (false, 6);
			box.PackStart (scrolled, true, true, 0);
			box.PackStart (check, false, false, 0);
			box.ShowAll ();

			value = val;

			PopulateTextView ();
			
			return box;
		}
		
		public bool StoreValue (ObjectValue val)
		{
			switch (val.TypeName) {
			case "char[]":
				val.SetRawValue (textView.Buffer.Text.ToCharArray ());
				return true;
			case "string":
				val.SetRawValue (textView.Buffer.Text);
				return true;
			default:
				return false;
			}
		}
		
		public bool CanEdit (ObjectValue val)
		{
			return val.TypeName == "string" || val.TypeName == "char[]";
		}
	}
}
