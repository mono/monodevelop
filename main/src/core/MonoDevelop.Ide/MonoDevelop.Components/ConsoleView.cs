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
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public class ConsoleView: ScrolledWindow
	{
		string scriptLines = "";
		
		Stack<string> commandHistoryPast = new Stack<string> ();
		Stack<string> commandHistoryFuture = new Stack<string> ();

		bool inBlock = false;
		string blockText = "";

		TextMark inputBeginMark;
		TextView textView;
	
		public ConsoleView ()
		{
			PromptString = "> ";
			PromptMultiLineString = ">> ";
			
			textView = new TextView ();
			Add (textView);
			ShowAll ();
			
			textView.WrapMode = Gtk.WrapMode.Word;
			textView.KeyPressEvent += TextViewKeyPressEvent;
			textView.PopulatePopup += TextViewPopulatePopup;

			inputBeginMark = Buffer.CreateMark (null, Buffer.EndIter, true);

			// The 'Freezer' tag is used to keep everything except
			// the input line from being editable
			TextTag tag = new TextTag ("Freezer");
			tag.Editable = false;
			Buffer.TagTable.Add (tag);
			Prompt (false);
		}

		void TextViewPopulatePopup (object o, PopulatePopupArgs args)
		{
			MenuItem item = new MenuItem (GettextCatalog.GetString ("Clear"));
			SeparatorMenuItem sep = new SeparatorMenuItem ();
			
			item.Activated += ClearActivated;
			item.Show ();
			sep.Show ();
			
			args.Menu.Add (sep);
			args.Menu.Add (item);
		}

		void ClearActivated (object sender, EventArgs e)
		{
			Clear ();
		}
		
		public void SetFont (Pango.FontDescription font)
		{
			textView.ModifyFont (font);
		}

		protected TextView TextView {
			get { return textView; }
		}
		
		public string PromptString { get; set; }
		
		public bool AutoIndent { get; set; }

		public string PromptMultiLineString { get; set; }
		
		[GLib.ConnectBeforeAttribute]
		void TextViewKeyPressEvent (object o, KeyPressEventArgs args)
		{
			if (ProcessKeyPressEvent (args))
				args.RetVal = true;
		}

		bool ProcessReturn ()
		{
			if (inBlock) {
				if (InputLine == "") {
					ProcessInput (blockText);
					blockText = "";
					inBlock = false;
				} else {
					blockText += "\n" + InputLine;
					string whiteSpace = null;
					if (AutoIndent) {
						System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex (@"^(\s+).*");
						whiteSpace = r.Replace (InputLine, "$1");
						if (InputLine.EndsWith (BlockStart, StringComparison.InvariantCulture))
							whiteSpace += "\t";
					}
					Prompt (true, true);
					if (AutoIndent)
						InputLine += whiteSpace;
				}
			} else {
				// Special case for start of new code block
				if (!string.IsNullOrEmpty (BlockStart) && InputLine.Trim().EndsWith (BlockStart, StringComparison.InvariantCulture)) {
					inBlock = true;
					blockText = InputLine;
					Prompt (true, true);
					if (AutoIndent)
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

		bool ProcessCommandHistoryUp ()
		{
			if (!inBlock && commandHistoryPast.Count > 0) {
				if (commandHistoryFuture.Count == 0) {
					commandHistoryFuture.Push (InputLine);
				} else {
					if (commandHistoryPast.Count == 1)
						return true;

					commandHistoryFuture.Push (commandHistoryPast.Pop ());
				}

				InputLine = commandHistoryPast.Peek ();
			}

			return true;
		}

		bool ProcessCommandHistoryDown ()
		{
			if (!inBlock && commandHistoryFuture.Count > 0) {
				if (commandHistoryFuture.Count == 1) {
					InputLine = commandHistoryFuture.Pop ();
				} else {
					commandHistoryPast.Push (commandHistoryFuture.Pop ());
					InputLine = commandHistoryPast.Peek ();
				}
			}

			return true;
		}
		
		protected virtual bool ProcessKeyPressEvent (KeyPressEventArgs args)
		{
			// Short circuit to avoid getting moved back to the input line
			// when paging up and down in the shell output
			if (args.Event.Key == Gdk.Key.Page_Up || args.Event.Key == Gdk.Key.Page_Down)
				return false;
			
			// Needed so people can copy and paste, but always end up
			// typing in the prompt.
			if (Cursor.Compare (InputLineBegin) < 0) {
				Buffer.MoveMark (Buffer.SelectionBound, InputLineEnd);
				Buffer.MoveMark (Buffer.InsertMark, InputLineEnd);
			}
			
//			if (ev.State == Gdk.ModifierType.ControlMask && ev.Key == Gdk.Key.space)
//				TriggerCodeCompletion ();

			switch (args.Event.Key) {
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				return ProcessReturn ();
			case Gdk.Key.KP_Up:
			case Gdk.Key.Up:
				return ProcessCommandHistoryUp ();
			case Gdk.Key.KP_Down:
			case Gdk.Key.Down:
				return ProcessCommandHistoryDown ();
			case Gdk.Key.KP_Left:
			case Gdk.Key.Left:
				// On Mac, when using a small keyboard, Home is Command+Left
				if (Platform.IsMac && args.Event.State.HasFlag (Gdk.ModifierType.MetaMask)) {
					Buffer.MoveMark (Buffer.InsertMark, InputLineBegin);

					// Move the selection mark too, if shift isn't held
					if (!args.Event.State.HasFlag (Gdk.ModifierType.ShiftMask))
						Buffer.MoveMark (Buffer.SelectionBound, InputLineBegin);

					return true;
				}

				// Keep our cursor inside the prompt area
				if (Cursor.Compare (InputLineBegin) <= 0)
					return true;

				break;
			case Gdk.Key.KP_Home:
			case Gdk.Key.Home:
				Buffer.MoveMark (Buffer.InsertMark, InputLineBegin);

				// Move the selection mark too, if shift isn't held
				if (!args.Event.State.HasFlag (Gdk.ModifierType.ShiftMask))
					Buffer.MoveMark (Buffer.SelectionBound, InputLineBegin);

				return true;
			case Gdk.Key.a:
				if (args.Event.State.HasFlag (Gdk.ModifierType.ControlMask)) {
					Buffer.MoveMark (Buffer.InsertMark, InputLineBegin);

					// Move the selection mark too, if shift isn't held
					if (!args.Event.State.HasFlag (Gdk.ModifierType.ShiftMask))
						Buffer.MoveMark (Buffer.SelectionBound, InputLineBegin);

					return true;
				}
				break;
			case Gdk.Key.period:
				return false;
			default:
				return false;
			}
			
			return false;
		}

		public TextIter InputLineBegin {
			get { return Buffer.GetIterAtMark (inputBeginMark); }
		}
	
		public TextIter InputLineEnd {
			get { return Buffer.EndIter; }
		}
		
		public TextIter Cursor {
			get { return Buffer.GetIterAtMark (Buffer.InsertMark); }
		}
		
		public Gtk.TextBuffer Buffer {
			get { return textView.Buffer; }
		}

		protected Gdk.Rectangle GetIterLocation (TextIter iter)
		{
			return textView.GetIterLocation (iter);
		}
		
		// The current input line
		public string InputLine {
			get {
				return Buffer.GetText (InputLineBegin, InputLineEnd, false);
			}
			set {
				TextIter start = InputLineBegin;
				TextIter end = InputLineEnd;
				Buffer.Delete (ref start, ref end);
				start = InputLineBegin;
				Buffer.Insert (ref start, value);
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
			TextIter start = Buffer.EndIter;
			Buffer.Insert (ref start , line);
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
		}
	
		public void Prompt (bool newLine)
		{
			Prompt (newLine, false);
		}
	
		public void Prompt (bool newLine, bool multiline)
		{
			TextIter end = Buffer.EndIter;
			if (newLine)
				Buffer.Insert (ref end, "\n");
			if (multiline)
				Buffer.Insert (ref end, PromptMultiLineString);
			else
				Buffer.Insert (ref end, PromptString);
	
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
	
			UpdateInputLineBegin ();
	
			// Freeze all the text except our input line
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);
		}

		protected virtual void UpdateInputLineBegin ()
		{
			// Record the end of where we processed, used to calculate start
			// of next input line
			Buffer.MoveMark (inputBeginMark, Buffer.EndIter);
		}
		
		public void Clear ()
		{
			Buffer.Text = "";
			scriptLines = "";
			Prompt (false);
		}
		
		public void ClearHistory ()
		{
			commandHistoryFuture.Clear ();
			commandHistoryPast.Clear ();
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
