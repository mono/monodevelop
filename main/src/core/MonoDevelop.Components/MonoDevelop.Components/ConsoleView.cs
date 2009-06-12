// 
// ConsoleView.cs
//  
// Author:
//       Peter Johanson <latexer@gentoo.org>
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using Gtk;

namespace MonoDevelop.Components
{
	public class ConsoleView: ScrolledWindow
	{
		string scriptLines = "";
		
		Stack<string> commandHistoryPast = new Stack<string> ();
		Stack<string> commandHistoryFuture = new Stack<string> ();
		
		bool inBlock = false;
		string blockText = "";
	
		bool reset_clears_history;
		bool reset_clears_scrollback;
		bool auto_indent;
		
		TextView textView;
	
		public ConsoleView()
		{
			PromptString = "> ";
			PromptMultiLineString = ">> ";
			
			textView = new TextView ();
			Add (textView);
			ShowAll ();
			
			textView.WrapMode = Gtk.WrapMode.Word;
			textView.KeyPressEvent += TextViewKeyPressEvent;
	
			// The 'Freezer' tag is used to keep everything except
			// the input line from being editable
			TextTag tag = new TextTag ("Freezer");
			tag.Editable = false;
			Buffer.TagTable.Add (tag);
			Prompt (false);
		}
		
		public void SetFont (Pango.FontDescription font)
		{
			textView.ModifyFont (font);
		}
		
		public string PromptString { get; set; }

		public string PromptMultiLineString { get; set; }
		
		[GLib.ConnectBeforeAttribute]
		void TextViewKeyPressEvent (object o, KeyPressEventArgs args)
		{
			args.RetVal = ProcessKeyPressEvent (args.Event);
		}
		
		bool ProcessKeyPressEvent (Gdk.EventKey ev)
		{
			// Short circuit to avoid getting moved back to the input line
			// when paging up and down in the shell output
			if (ev.Key == Gdk.Key.Page_Up || ev.Key == Gdk.Key.Page_Down)
				return false;
			
			// Needed so people can copy and paste, but always end up
			// typing in the prompt.
			if (Cursor.Compare (InputLineBegin) < 0) {
				Buffer.MoveMark (Buffer.SelectionBound, InputLineEnd);
				Buffer.MoveMark (Buffer.InsertMark, InputLineEnd);
			}
			
//			if (ev.State == Gdk.ModifierType.ControlMask && ev.Key == Gdk.Key.space)
//				TriggerCodeCompletion ();
	
			if (ev.Key == Gdk.Key.Return) {
				if (inBlock) {
					if (InputLine == "") {
						ProcessInput (blockText);
						blockText = "";
						inBlock = false;
					} else {
						blockText += "\n" + InputLine;
						string whiteSpace = null;
						if (auto_indent) {
							System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex (@"^(\s+).*");
							whiteSpace = r.Replace (InputLine, "$1");
							if (InputLine.EndsWith (BlockStart))
								whiteSpace += "\t";
						}
						Prompt (true, true);
						if (auto_indent)
							InputLine += whiteSpace;
					}
				} else {
					// Special case for start of new code block
					if (!string.IsNullOrEmpty (BlockStart) && InputLine.Trim().EndsWith (BlockStart)) {
						inBlock = true;
						blockText = InputLine;
						Prompt (true, true);
						if (auto_indent)
							InputLine += "\t";
						return true;
					}
					// Bookkeeping
					if (InputLine != "") {
						// Everything but the last item (which was input),
						//in the future stack needs to get put back into the
						// past stack
						while (commandHistoryFuture.Count > 1)
							commandHistoryPast.Push (commandHistoryFuture.Pop());
						// Clear the pesky junk input line
						commandHistoryFuture.Clear();
	
						// Record our input line
						commandHistoryPast.Push(InputLine);
						if (scriptLines == "")
							scriptLines += InputLine;
						else
							scriptLines += "\n" + InputLine;
					
						ProcessInput (InputLine);
					}
				}
				return true;
			}
	
			// The next two cases handle command history	
			else if (ev.Key == Gdk.Key.Up) {
				if (!inBlock && commandHistoryPast.Count > 0) {
					if (commandHistoryFuture.Count == 0)
						commandHistoryFuture.Push (InputLine);
					else {
						if (commandHistoryPast.Count == 1)
							return true;
						commandHistoryFuture.Push (commandHistoryPast.Pop());
					}
					InputLine = commandHistoryPast.Peek();
				}
				return true;
			}
			else if (ev.Key == Gdk.Key.Down) {
				if (!inBlock && commandHistoryFuture.Count > 0) {
					if (commandHistoryFuture.Count == 1)
						InputLine = commandHistoryFuture.Pop();
					else {
						commandHistoryPast.Push (commandHistoryFuture.Pop ());
						InputLine = commandHistoryPast.Peek ();
					}
				}
				return true;
			}	
			else if (ev.Key == Gdk.Key.Left) {
				// Keep our cursor inside the prompt area
				if (Cursor.Compare (InputLineBegin) <= 0)
					return true;
			}
			else if (ev.Key == Gdk.Key.Home) {
				Buffer.MoveMark (Buffer.InsertMark, InputLineBegin);
				// Move the selection mark too, if shift isn't held
				if ((ev.State & Gdk.ModifierType.ShiftMask) == ev.State)
					Buffer.MoveMark (Buffer.SelectionBound, InputLineBegin);
				return true;
			}
			else if (ev.Key == Gdk.Key.period) {
				return false;
			}
	
			// Short circuit to avoid getting moved back to the input line
			// when paging up and down in the shell output
			else if (ev.Key == Gdk.Key.Page_Up || ev.Key == Gdk.Key.Page_Down) {
				return false;
			}
			
			return false;
		}
		
		TextMark endOfLastProcessing;

		public TextIter InputLineBegin {
			get {
				return Buffer.GetIterAtMark (endOfLastProcessing);
			}
		}
	
		public TextIter InputLineEnd {
			get { return Buffer.EndIter; }
		}
		
		private TextIter Cursor {
			get { return Buffer.GetIterAtMark (Buffer.InsertMark); }
		}
		
		Gtk.TextBuffer Buffer {
			get { return textView.Buffer; }
		}
		
		// The current input line
		public string InputLine {
			get {
				return Buffer.GetText (InputLineBegin, InputLineEnd, false);
			}
			set {
				TextIter start = InputLineBegin;
				TextIter end = InputLineEnd;
				Buffer.Delete (start, end);
				start = InputLineBegin;
				Buffer.Insert (start, value);
			}
		}
		
		protected virtual void ProcessInput (string line)
		{
			WriteOutput ("\n");
			if (ConsoleInput != null)
				ConsoleInput (this, new ConsoleInputEventArgs (line));
		}
		
		public void WriteOutput (string line)
		{
			Buffer.Insert (Buffer.EndIter , line);
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
		}
	
		public void Prompt (bool newLine)
		{
			Prompt (newLine, false);
		}
	
		public void Prompt (bool newLine, bool multiline)
		{
			if (newLine)
				Buffer.Insert (Buffer.EndIter , "\n");
			if (multiline)
				Buffer.Insert (Buffer.EndIter , PromptMultiLineString);
			else
				Buffer.Insert (Buffer.EndIter , PromptString);
	
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
	
			// Record the end of where we processed, used to calculate start
			// of next input line
			endOfLastProcessing = Buffer.CreateMark (null, Buffer.EndIter, true);
	
			// Freeze all the text except our input line
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);
		}
		
		public void Reset()
		{
			if (reset_clears_scrollback)
				Buffer.Text = "";
			if (reset_clears_history) {
				commandHistoryFuture.Clear ();
				commandHistoryPast.Clear ();
			}
	
			scriptLines = "";
			Prompt (!reset_clears_scrollback);
		}
		
		public string BlockStart { get; set; }
		
		public string BlockEnd { get; set; }
		
		public event EventHandler<ConsoleInputEventArgs> ConsoleInput;
	}
	
	public class ConsoleInputEventArgs: EventArgs
	{
		public ConsoleInputEventArgs (string text)
		{
			Text = text;
		}
		
		public string Text { get; internal set; }
	}
}
