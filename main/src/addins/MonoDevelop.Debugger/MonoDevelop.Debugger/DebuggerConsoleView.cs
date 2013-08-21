//
// DebuggerConsoleView.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (www.xamarin.com)
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

using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Debugger
{
	public class DebuggerConsoleView : ConsoleView, ICompletionWidget
	{
		Mono.Debugging.Client.CompletionData currentCompletionData;
		Gtk.TextMark tokenBeginMark;
		CodeCompletionContext ctx;
		Gdk.ModifierType modifier;
		bool keyHandled = false;
		uint keyValue;
		char keyChar;
		Gdk.Key key;

		public DebuggerConsoleView ()
		{
			SetFont (IdeApp.Preferences.CustomOutputPadFont);

			TextView.KeyReleaseEvent += OnEditKeyRelease;

			IdeApp.Preferences.CustomOutputPadFontChanged += OnCustomOutputPadFontChanged;
			CompletionWindowManager.WindowClosed += OnCompletionWindowClosed;
		}

		static bool IsCompletionChar (char c)
		{
			return (char.IsLetterOrDigit (c) || char.IsPunctuation (c) || char.IsSymbol (c) || char.IsWhiteSpace (c));
		}

		static Mono.Debugging.Client.CompletionData GetCompletionData (string exp)
		{
			if (DebuggingService.CurrentFrame != null)
				return DebuggingService.CurrentFrame.GetExpressionCompletionData (exp);

			return null;
		}

		void OnCompletionWindowClosed (object sender, EventArgs e)
		{
			currentCompletionData = null;
		}

		void PopupCompletion ()
		{
			Gtk.Application.Invoke (delegate {
				char c = (char) Gdk.Keyval.ToUnicode (keyValue);
				if (currentCompletionData == null && IsCompletionChar (c)) {
					string expr = Buffer.GetText (TokenBegin, Cursor, false);
					currentCompletionData = GetCompletionData (expr);
					if (currentCompletionData != null) {
						DebugCompletionDataList dataList = new DebugCompletionDataList (currentCompletionData);
						ctx = ((ICompletionWidget) this).CreateCodeCompletionContext (expr.Length - currentCompletionData.ExpressionLength);
						CompletionWindowManager.ShowWindow (null, c, dataList, this, ctx);
					} else {
						currentCompletionData = null;
					}
				}
			});
		}

		static bool EatWhitespace (string text, ref int index)
		{
			while (index < text.Length && char.IsWhiteSpace (text[index]))
				index++;

			return index < text.Length;
		}

		static bool EatIdentifier (string text, ref int index)
		{
			int startIndex = index;

			if (index >= text.Length)
				return false;

			while (index < text.Length && (char.IsLetterOrDigit (text[index]) || text[index] == '_'))
				index++;

			return index > startIndex;
		}

		static bool EatLiteralString (string text, ref int index)
		{
			// skip over the '@'
			index++;

			if (index >= text.Length || text[index] != '"')
				return false;

			// skip over the double quotes
			index++;

			while (index < text.Length) {
				if (text[index++] == '"' && index < text.Length && text[index] == '"')
					index++;
			}

			return index < text.Length;
		}

		static bool EatQuotedString (string text, ref int index)
		{
			char quote = text[index++];
			bool escaped = false;

			while (index < text.Length) {
				if (escaped) {
					escaped = false;
				} else if (text[index] == '\\') {
					escaped = true;
				} else if (text[index] == quote) {
					index++;
					break;
				}

				index++;
			}

			return index < text.Length;
		}

		static readonly string[] SyntaxTokens = new string[] {
			"=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "~=",
			"+", "-", "*", "/", "%", "&", "|", "~",
			"==", "!=", ">", ">=", "<", "<=",
			"(", ")", "[", "]", ","
		};

		static string ReadSyntaxToken (string text, ref int index)
		{
			if (index + 1 >= text.Length)
				return null;

			string subtext = text.Substring (index, Math.Min (text.Length - index, 2));
			string token = null;
			int matchLen = 0;

			for (int i = 0; i < SyntaxTokens.Length && matchLen < 2; i++) {
				if (subtext.StartsWith (SyntaxTokens[i], StringComparison.Ordinal)) {
					if (SyntaxTokens[i].Length > matchLen) {
						token = SyntaxTokens[i];
						matchLen = token.Length;
					}
				}
			}

			if (token != null)
				index += matchLen;

			return token;
		}

		void UpdateTokenBeginMarker ()
		{
			var text = Buffer.GetText (InputLineBegin, Cursor, false);
			var tokens = new Stack<string> ();
			var stack = new Stack<int> ();
			int index = 0;
			string token;

			if (!EatWhitespace (text, ref index))
				return;

			stack.Push (index);

			while (EatIdentifier (text, ref index) && EatWhitespace (text, ref index)) {
				if (text[index] == '.') {
					index++;

					continue;
				}

				if (text[index] == '@') {
					if (!EatLiteralString (text, ref index))
						break;

					continue;
				}

				if (text[index] == '"' || text[index] == '\'') {
					if (!EatQuotedString (text, ref index))
						break;

					continue;
				}

				while ((token = ReadSyntaxToken (text, ref index)) != null) {
					EatWhitespace (text, ref index);

					switch (token) {
					case ")": case "]":
						if (tokens.Contains (token)) {
							do {
								stack.Pop ();
							} while (tokens.Pop () != token);
						}
						break;
					case "(":
						tokens.Push (")");
						stack.Push (index);
						break;
					case "[":
						tokens.Push ("]");
						stack.Push (index);
						break;
					default:
						tokens.Push (token);
						stack.Push (index);
						break;
					}
				}
			}

			index = stack.Peek ();

			var iter = Buffer.GetIterAtOffset (InputLineBegin.Offset + index);
			Buffer.MoveMark (tokenBeginMark, iter);
		}

		void OnEditKeyRelease (object sender, Gtk.KeyReleaseEventArgs args)
		{
			UpdateTokenBeginMarker ();

			if (keyHandled)
				return;

			string text = TokenText;

			if (ctx != null)
				text = text.Substring (Math.Max (0, Math.Min (ctx.TriggerOffset, text.Length)));

			CompletionWindowManager.UpdateWordSelection (text);
			CompletionWindowManager.PostProcessKeyEvent (key, keyChar, modifier);
			PopupCompletion ();
		}

		protected override bool ProcessKeyPressEvent (Gtk.KeyPressEventArgs args)
		{
			keyHandled = false;

			keyChar = (char) args.Event.Key;
			keyValue = args.Event.KeyValue;
			modifier = args.Event.State;
			key = args.Event.Key;

			if ((args.Event.Key == Gdk.Key.Down || args.Event.Key == Gdk.Key.Up)) {
				keyChar = '\0';
			}

			if (currentCompletionData != null) {
				if ((keyHandled = CompletionWindowManager.PreProcessKeyEvent (key, keyChar, modifier)))
					return true;
			}

			return base.ProcessKeyPressEvent (args);
		}

		protected override void UpdateInputLineBegin ()
		{
			if (tokenBeginMark == null)
				tokenBeginMark = Buffer.CreateMark (null, Buffer.EndIter, true);
			else
				Buffer.MoveMark (tokenBeginMark, Buffer.EndIter);

			base.UpdateInputLineBegin ();
		}

		string TokenText {
			get { return Buffer.GetText (TokenBegin, TokenEnd, false); }
			set {
				var start = TokenBegin;
				var end = TokenEnd;

				Buffer.Delete (ref start, ref end);
				start = TokenBegin;
				Buffer.Insert (ref start, value);
			}
		}

		Gtk.TextIter TokenBegin {
			get { return Buffer.GetIterAtMark (tokenBeginMark); }
		}

		Gtk.TextIter TokenEnd {
			get { return Cursor; }
		}

		int Position {
			get { return Cursor.Offset - TokenBegin.Offset; }
		}

		#region ICompletionWidget implementation

		CodeCompletionContext ICompletionWidget.CurrentCodeCompletionContext {
			get {
				return ((ICompletionWidget) this).CreateCodeCompletionContext (Position);
			}
		}

		EventHandler completionContextChanged;

		event EventHandler ICompletionWidget.CompletionContextChanged {
			add { completionContextChanged += value; }
			remove { completionContextChanged -= value; }
		}

		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			var text = TokenText;

			if (startOffset < 0 || startOffset > text.Length) startOffset = 0;
			if (endOffset > text.Length) endOffset = text.Length;

			return text.Substring (startOffset, endOffset - startOffset);
		}

		void ICompletionWidget.Replace (int offset, int count, string text)
		{
			if (count > 0)
				TokenText = TokenText.Remove (offset, count);
			if (!string.IsNullOrEmpty (text))
				TokenText = TokenText.Insert (offset, text);
		}

		int ICompletionWidget.CaretOffset {
			get {
				return Position;
			}
		}

		char ICompletionWidget.GetChar (int offset)
		{
			string text = TokenText;

			if (offset >= text.Length)
				return (char) 0;

			return text[offset];
		}

		CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
		{
			CodeCompletionContext c = new CodeCompletionContext ();
			c.TriggerLine = 0;
			c.TriggerOffset = triggerOffset;
			c.TriggerLineOffset = c.TriggerOffset;
			c.TriggerWordLength = currentCompletionData.ExpressionLength;

			int height, lineY, x, y;
			TextView.GdkWindow.GetOrigin (out x, out y);
			TextView.GetLineYrange (Cursor, out lineY, out height);

			var rect = GetIterLocation (Cursor);

			c.TriggerYCoord = y + lineY + height;
			c.TriggerXCoord = x + rect.X;
			c.TriggerTextHeight = height;

			return c;
		}

		string ICompletionWidget.GetCompletionText (CodeCompletionContext ctx)
		{
			return TokenText.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}

		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int sp = Position - partial_word.Length;

			var start = Buffer.GetIterAtOffset (TokenBegin.Offset + sp);
			var end = Buffer.GetIterAtOffset (start.Offset + partial_word.Length);
			Buffer.Delete (ref start, ref end);
			Buffer.Insert (ref start, complete_word);
			Buffer.PlaceCursor (start);
		}

		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int offset)
		{
			int sp = Position - partial_word.Length;

			var start = Buffer.GetIterAtOffset (TokenBegin.Offset + sp);
			var end = Buffer.GetIterAtOffset (start.Offset + partial_word.Length);
			Buffer.Delete (ref start, ref end);
			Buffer.Insert (ref start, complete_word);

			var cursor = Buffer.GetIterAtOffset (start.Offset + offset);
			Buffer.PlaceCursor (cursor);
		}

		int ICompletionWidget.TextLength {
			get {
				return TokenText.Length;
			}
		}

		int ICompletionWidget.SelectedLength {
			get {
				return 0;
			}
		}

		Gtk.Style ICompletionWidget.GtkStyle {
			get {
				return Style;
			}
		}

		#endregion

		void OnCustomOutputPadFontChanged (object sender, EventArgs e)
		{
			SetFont (IdeApp.Preferences.CustomOutputPadFont);
		}

		protected override void OnDestroyed ()
		{
			IdeApp.Preferences.CustomOutputPadFontChanged -= OnCustomOutputPadFontChanged;
			CompletionWindowManager.WindowClosed -= OnCompletionWindowClosed;
			CompletionWindowManager.HideWindow ();
			base.OnDestroyed ();
		}
	}
}
