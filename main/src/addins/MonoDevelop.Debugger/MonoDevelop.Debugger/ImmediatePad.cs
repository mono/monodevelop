// 
// ImmediatePad.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
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

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Debugger
{
	public class ImmediatePad: IPadContent
	{
		ImmediateConsoleView view;
		
		public void Initialize (IPadWindow container)
		{
			view = new ImmediateConsoleView ();
			view.ConsoleInput += OnViewConsoleInput;
			view.ShadowType = Gtk.ShadowType.None;
			view.ShowAll ();
		}

		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			if (!DebuggingService.IsDebugging) {
				view.WriteOutput ("Debug session not started.");
			} else if (DebuggingService.IsRunning) {
				view.WriteOutput ("The expression can't be evaluated while the application is running.");
			} else {
				EvaluationOptions ops = EvaluationOptions.DefaultOptions;
				var frame = DebuggingService.CurrentFrame;
				string expression = e.Text;

				ops.AllowMethodEvaluation = true;
				ops.AllowToStringCalls = true;
				ops.AllowTargetInvoke = true;
				ops.EvaluationTimeout = 20000;
				ops.EllipsizeStrings = false;

				var vres = frame.ValidateExpression (expression, ops);
				if (!vres) {
					view.WriteOutput (vres.Message);
					view.Prompt (true);
					return;
				}

				var val = frame.GetExpressionValue (expression, ops);
				if (val.IsEvaluating) {
					WaitForCompleted (val);
					return;
				}

				PrintValue (val);
			}
			view.Prompt (true);
		}
		
		void PrintValue (ObjectValue val)
		{
			string result = val.Value;
			if (string.IsNullOrEmpty (result)) {
				if (val.IsNotSupported)
					result = GettextCatalog.GetString ("Expression not supported.");
				else if (val.IsError || val.IsUnknown)
					result = GettextCatalog.GetString ("Evaluation failed.");
				else
					result = string.Empty;
			}
			view.WriteOutput (result);
		}
		
		void WaitForCompleted (ObjectValue val)
		{
			int iteration = 0;
			
			GLib.Timeout.Add (100, delegate {
				if (!val.IsEvaluating) {
					if (iteration >= 5)
						view.WriteOutput ("\n");
					PrintValue (val);
					view.Prompt (true);
					return false;
				}
				if (++iteration == 5)
					view.WriteOutput (GettextCatalog.GetString ("Evaluating") + " ");
				else if (iteration > 5 && (iteration - 5) % 10 == 0)
					view.WriteOutput (".");
				else if (iteration > 300) {
					view.WriteOutput ("\n" + GettextCatalog.GetString ("Timed out."));
					view.Prompt (true);
					return false;
				}
				return true;
			});
		}

		public void RedrawContent ()
		{
		}
		
		public Gtk.Widget Control {
			get {
				return view;
			}
		}

		public void Dispose ()
		{
		}
	}

	class ImmediateConsoleView : ConsoleView, ICompletionWidget
	{
		const string tokenBeginChars = "([<,";

		Mono.Debugging.Client.CompletionData currentCompletionData;
		bool isLiteralString = false;
		Gtk.TextMark tokenBeginMark;
		CodeCompletionContext ctx;
		Gdk.ModifierType modifier;
		bool keyHandled = false;
		uint keyValue;
		char keyChar;
		Gdk.Key key;

		public ImmediateConsoleView ()
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

		void OnEditKeyRelease (object sender, Gtk.KeyReleaseEventArgs args)
		{
			if (!isLiteralString && (tokenBeginChars.IndexOf (keyChar) != -1 || (keyChar == ' ' && TokenBegin.Offset + 1 == Cursor.Offset)))
				Buffer.MoveMark (tokenBeginMark, Cursor);

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

			if (keyChar == '"')
				isLiteralString = !isLiteralString;

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
			isLiteralString = false;
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
			get { return InputLineEnd; }
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
