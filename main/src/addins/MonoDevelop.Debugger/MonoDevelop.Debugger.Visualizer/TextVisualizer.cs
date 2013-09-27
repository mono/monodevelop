// 
// TextVisualizer.cs
//  
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
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
	public class TextVisualizer: ValueVisualizer
	{
		const int CHUNK_SIZE = 1024;

		RawValueString rawString;
		RawValueArray rawArray;
		TextView textView;
		uint idle_id;
		int length;
		int offset;
		
		public TextVisualizer ()
		{
		}
		
		public override string Name {
			get { return GettextCatalog.GetString ("Text"); }
		}
		
		public override bool CanVisualize (ObjectValue val)
		{
			switch (val.TypeName) {
			case "char[]": return true;
			case "string": return true;
			default: return false;
			}
		}

		public override bool IsDefaultVisualizer (ObjectValue val)
		{
			return true;
		}

		bool GetNextCharArrayChunk ()
		{
			int amount = Math.Min (length - offset, CHUNK_SIZE);
			var chunk = rawArray.GetValues (offset, amount) as char[];
			var iter = textView.Buffer.EndIter;

			textView.Buffer.Insert (ref iter, new string (chunk));
			offset += amount;

			if (offset < length)
				return true;

			idle_id = 0;

			// Remove this idle callback
			return false;
		}
		
		bool GetNextStringChunk ()
		{
			int amount = Math.Min (length - offset, CHUNK_SIZE);
			string chunk = rawString.Substring (offset, amount);
			TextIter iter = textView.Buffer.EndIter;
			
			textView.Buffer.Insert (ref iter, chunk);
			offset += amount;
			
			if (offset < length)
				return true;
			
			idle_id = 0;
			
			// Remove this idle callback
			return false;
		}

		void PopulateTextView (ObjectValue value)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.ChunkRawStrings = true;

			if (value.TypeName == "string") {
				rawString = value.GetRawValue (ops) as RawValueString;
				length = rawString.Length;
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
			} else if (value.TypeName == "char[]") {
				rawArray = value.GetRawValue () as RawValueArray;
				length = rawArray.Length;
				offset = 0;

				if (length > 0) {
					idle_id = GLib.Idle.Add (GetNextCharArrayChunk);
					textView.Destroyed += delegate {
						if (idle_id != 0) {
							GLib.Source.Remove (idle_id);
							idle_id = 0;
						}
					};
				}
			}
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
		
		public override bool StoreValue (ObjectValue val)
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
		
		public override bool CanEdit (ObjectValue val)
		{
			switch (val.TypeName) {
			case "char[]": return true;
			case "string": return true;
			default: return false;
			}
		}
	}
}
