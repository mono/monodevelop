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
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.Debugger
{
	public class ImmediatePad: IPadContent
	{
		static object locker = new object();
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
				view.WriteOutput (GettextCatalog.GetString ("Debug session not started."));
				FinishPrinting ();
			} else if (DebuggingService.IsRunning) {
				view.WriteOutput (GettextCatalog.GetString ("The expression can't be evaluated while the application is running."));
				FinishPrinting ();
			} else {

				var frame = DebuggingService.CurrentFrame;
				string expression = e.Text;

				EvaluationOptions ops = GetEvaluationOptions ();
				var vres = frame.ValidateExpression (expression, ops);
				if (!vres) {
					view.WriteOutput (vres.Message);
					FinishPrinting ();
					return;
				}

				var val = frame.GetExpressionValue (expression, ops);
				if (val.IsEvaluating) {
					WaitForCompleted (val);
					return;
				}
				PrintValue (val);
			}
		}	

		EvaluationOptions GetEvaluationOptions ()
		{
			EvaluationOptions ops = EvaluationOptions.DefaultOptions;
			ops.AllowMethodEvaluation = true;
			ops.AllowToStringCalls = true;
			ops.AllowTargetInvoke = true;
			ops.EvaluationTimeout = 20000;
			ops.EllipsizeStrings = false;
			ops.MemberEvaluationTimeout = 20000;
			return ops;
		}

		string GetErrorText (ObjectValue val)
		{
			if (val.IsNotSupported)
				return string.IsNullOrEmpty(val.Value) ? GettextCatalog.GetString ("Expression not supported.") : val.Value;
			else if (val.IsError || val.IsUnknown)
				return string.IsNullOrEmpty(val.Value) ? GettextCatalog.GetString ("Evaluation failed.") : val.Value;
			else
				return string.Empty;
		}

		void PrintValue (ObjectValue val) 
		{
			string result = val.Value;
			if (string.IsNullOrEmpty (result) || val.IsError || val.IsUnknown || val.IsNotSupported) {
				view.WriteOutput (GetErrorText (val));
				FinishPrinting ();
			} else {
				view.WriteOutput (result);
				EvaluationOptions ops = GetEvaluationOptions ();
				var children = val.GetAllChildren (ops);
				var hasMore = false;
				if (children.Length > 0 && string.Equals(children[0].Name, "[0..99]")) { //Big Arrays Hack
					children = children [0].GetAllChildren ();
					hasMore = true;
				}
				var evaluating = new Dictionary<ObjectValue, bool> ();
				foreach (var child in children) {
					if (child.IsEvaluating) {
						evaluating.Add (child, false);
					} else {
						PrintChildValue (child);
					}
				}

				if (evaluating.Count > 0) {
					foreach (var eval in evaluating) {
						var eval1 = eval;
						WaitChildForCompleted (eval1.Key, evaluating, hasMore);
					}
				} else {
					FinishPrinting (hasMore);
				}
			}
		}

		void PrintChildValue (ObjectValue val)
		{
			view.WriteOutput (Environment.NewLine);
			string prefix = "\t" + val.Name + ": ";
			string result = val.Value;
			if (string.IsNullOrEmpty (result) || val.IsError || val.IsUnknown || val.IsNotSupported) {
				view.WriteOutput (prefix + GetErrorText (val));
			} else {
				view.WriteOutput (prefix + result);
			}
		}

		void PrintChildValueAtMark (ObjectValue val, Gtk.TextMark mark) 
		{
			string prefix = "\t" + val.Name + ": ";
			string result = val.Value; 
			if (string.IsNullOrEmpty (result) || val.IsError || val.IsUnknown || val.IsNotSupported) {
				SetLineText (prefix + GetErrorText (val), mark);
			} else {
				SetLineText (prefix + result, mark);
			}
		}

		void FinishPrinting(bool hasMore = false)
		{
			if (hasMore) {
				view.WriteOutput (Environment.NewLine + "\t" + string.Format(GettextCatalog.GetString ("< More... (The first {0} items were displayed.) >"), 100));
			}
			view.Prompt (true);
		}

		Gtk.TextIter DeleteLineAtMark (Gtk.TextMark mark)
		{
			var endIter = view.Buffer.GetIterAtMark(mark);
			endIter.ForwardLine ();
			var startIter = view.Buffer.GetIterAtMark (mark);
			view.Buffer.Delete (ref startIter, ref endIter);
			return startIter;
		}

		void SetLineText(string txt, Gtk.TextMark mark)
		{
			var startIter = DeleteLineAtMark (mark);
			view.Buffer.Insert (ref startIter, txt + Environment.NewLine);
		}

		void WaitForCompleted(ObjectValue val) {
			var mark = view.Buffer.CreateMark (null, view.InputLineEnd, true);
			var iteration = 0;
			GLib.Timeout.Add (100, () => {
				if (!val.IsEvaluating) {
					if (iteration >= 5) {
						DeleteLineAtMark (mark);
					}
					PrintValue (val);
					return false;
				} else {
					if (++iteration == 5) {
						SetLineText (GettextCatalog.GetString ("Evaluating"), mark);
					} else if (iteration > 5 && (iteration - 5) % 10 == 0) {
						string points = string.Join ("", Enumerable.Repeat (".", iteration / 10));
						SetLineText (GettextCatalog.GetString ("Evaluating") + " " + points, mark);
					}
					return true;
				}
			});
		}

		void WaitChildForCompleted (ObjectValue val, IDictionary<ObjectValue, bool> evaluatingList, bool hasMore)
		{
			view.WriteOutput (Environment.NewLine + " ");
			var mark = view.Buffer.CreateMark (null, view.InputLineEnd, true);
			var iteration = 0;
			GLib.Timeout.Add (100, () => {
				if (!val.IsEvaluating) {
					PrintChildValueAtMark (val, mark);
					lock(locker) { //Maybe We don't need this lock because children evaluation is done synchronously
						evaluatingList[val] = true;
						if (evaluatingList.All (x => x.Value)) {
							FinishPrinting (hasMore);
						}
					}
					return false;
				} else {
					string prefix = "\t" + val.Name + ": ";
					if (++iteration == 5) {
						SetLineText (prefix + GettextCatalog.GetString ("Evaluating"), mark);
					} else if (iteration > 5 && (iteration - 5) % 10 == 0) {
						string points = string.Join ("", Enumerable.Repeat (".", iteration / 10));
						SetLineText (prefix + GettextCatalog.GetString ("Evaluating") + " " + points, mark);
					}
					return true;
				}
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
