// ExpressionEvaluatorDialog.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Ide.CodeCompletion;
using Gtk;

namespace MonoDevelop.Debugger
{
	public partial class ExpressionEvaluatorDialog : Gtk.Dialog, ICompletionWidget
	{
		Mono.Debugging.Client.CompletionData currentCompletionData;

		public ExpressionEvaluatorDialog()
		{
			this.Build();
			valueTree.Frame = DebuggingService.CurrentFrame;
			valueTree.AllowExpanding = true;
			entry.KeyPressEvent += OnEditKeyPress;
			entry.FocusOutEvent += OnEditFocusOut;
			CompletionWindowManager.WindowClosed += HandleCompletionWindowClosed;
		}

		protected override void OnDestroyed ()
		{
			CompletionWindowManager.WindowClosed -= HandleCompletionWindowClosed;
			base.OnDestroyed ();
		}

		public string Expression {
			get { return entry.Text; }
			set {
				entry.Text = value;
				UpdateExpression ();
			}
		}
		
		void UpdateExpression ()
		{
			valueTree.ClearValues ();
			valueTree.ClearExpressions ();
			if (entry.Text.Length > 0)
				valueTree.AddExpression (entry.Text);
		}
		
		protected virtual void OnButtonEvalClicked (object sender, System.EventArgs e)
		{
			UpdateExpression ();
		}

		void HandleCompletionWindowClosed (object sender, EventArgs e)
		{
			currentCompletionData = null;
		}

		[GLib.ConnectBeforeAttribute]
		void OnEditKeyPress (object sender, KeyPressEventArgs args)
		{
			if (currentCompletionData != null) {
				bool ret = CompletionWindowManager.PreProcessKeyEvent (args.Event.Key, (char)args.Event.Key, args.Event.State);
				CompletionWindowManager.PostProcessKeyEvent (args.Event.Key, (char)args.Event.Key, args.Event.State);
				args.RetVal = ret;
			}
			
			Application.Invoke (delegate {
				char c = (char)Gdk.Keyval.ToUnicode (args.Event.KeyValue);
				if (currentCompletionData == null && IsCompletionChar (c)) {
					string exp = entry.Text.Substring (0, entry.CursorPosition);
					currentCompletionData = GetCompletionData (exp);
					if (currentCompletionData != null) {
						DebugCompletionDataList dataList = new DebugCompletionDataList (currentCompletionData);
						CodeCompletionContext ctx = ((ICompletionWidget)this).CreateCodeCompletionContext (entry.CursorPosition - currentCompletionData.ExpressionLength);
						CompletionWindowManager.ShowWindow (null, c, dataList, this, ctx);
					}
				}
			});
		}

		void OnEditFocusOut (object sender, FocusOutEventArgs args)
		{
			CompletionWindowManager.HideWindow ();
		}

		bool IsCompletionChar (char c)
		{
			return (char.IsLetterOrDigit (c) || char.IsPunctuation (c) || char.IsSymbol (c) || char.IsWhiteSpace (c));
		}

		Mono.Debugging.Client.CompletionData GetCompletionData (string exp)
		{
			if (valueTree.Frame != null)
				return valueTree.Frame.GetExpressionCompletionData (exp);
			else
				return null;
		}

		#region ICompletionWidget implementation 
		
		CodeCompletionContext ICompletionWidget.CurrentCodeCompletionContext {
			get {
				return ((ICompletionWidget)this).CreateCodeCompletionContext (entry.Position);
			}
		}
		
		EventHandler completionContextChanged;
		
		event EventHandler ICompletionWidget.CompletionContextChanged {
			add { completionContextChanged += value; }
			remove { completionContextChanged -= value; }
		}
		
		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			if (startOffset < 0) startOffset = 0;
			if (endOffset > entry.Text.Length) endOffset = entry.Text.Length;
			return entry.Text.Substring (startOffset, endOffset - startOffset);
		}
		
		void ICompletionWidget.Replace (int offset, int count, string text)
		{
			if (count > 0)
				entry.Text = entry.Text.Remove (offset, count);
			if (!string.IsNullOrEmpty (text))
				entry.Text = entry.Text.Insert (offset, text);
		}
		
		int ICompletionWidget.CaretOffset {
			get {
				return entry.Position;
			}
		}
		
		char ICompletionWidget.GetChar (int offset)
		{
			string txt = entry.Text;
			if (offset >= txt.Length)
				return (char)0;
			else
				return txt [offset];
		}
		
		CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
		{
			CodeCompletionContext c = new CodeCompletionContext ();
			c.TriggerLine = 0;
			c.TriggerOffset = triggerOffset;
			c.TriggerLineOffset = c.TriggerOffset;
			c.TriggerTextHeight = entry.SizeRequest ().Height;
			c.TriggerWordLength = currentCompletionData.ExpressionLength;
			
			int x, y;
			int tx, ty;
			entry.GdkWindow.GetOrigin (out x, out y);
			entry.GetLayoutOffsets (out tx, out ty);
			int cp = entry.TextIndexToLayoutIndex (entry.Position);
			Pango.Rectangle rect = entry.Layout.IndexToPos (cp);
			tx += Pango.Units.ToPixels (rect.X) + x;
			y += entry.Allocation.Height;
			
			c.TriggerXCoord = tx;
			c.TriggerYCoord = y;
			return c;
		}
		
		string ICompletionWidget.GetCompletionText (CodeCompletionContext ctx)
		{
			return entry.Text.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}
		
		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int sp = entry.Position - partial_word.Length;
			entry.DeleteText (sp, sp + partial_word.Length);
			entry.InsertText (complete_word, ref sp);
			entry.Position = sp; // sp is incremented by InsertText
		}
		
		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int offset)
		{
			int sp = entry.Position - partial_word.Length;
			entry.DeleteText (sp, sp + partial_word.Length);
			entry.InsertText (complete_word, ref sp);
			entry.Position = sp + offset; // sp is incremented by InsertText
		}
		
		int ICompletionWidget.TextLength {
			get {
				return entry.Text.Length;
			}
		}
		
		int ICompletionWidget.SelectedLength {
			get {
				return 0;
			}
		}
		
		Style ICompletionWidget.GtkStyle {
			get {
				return entry.Style;
			}
		}
		#endregion 
	}
}
