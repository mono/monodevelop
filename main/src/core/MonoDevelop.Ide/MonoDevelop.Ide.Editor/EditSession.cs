//
// EditSession.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor
{
	public abstract class EditSession : IDisposable
	{
		private TextEditor editor;
		ITextSourceVersion version;

		protected int startOffset, endOffset;

		public int StartOffset { get { return version.MoveOffsetTo (editor.Version, startOffset); } }

		public int EndOffset  { get { return version.MoveOffsetTo (editor.Version, endOffset); } }

		public TextEditor Editor {
			get {
				return editor;
			}
		}

		public EditSession()
		{
		}

		public EditSession (int startOffset, int endOffset)
		{
			this.startOffset = startOffset;
			this.endOffset = endOffset;
		}

		internal void SetEditor(TextEditor editor)
		{
			if (editor == null)
				throw new ArgumentNullException (nameof (editor));
			this.editor = editor;
			this.version = editor.Version;
			OnEditorSet ();
		}

		protected virtual void OnEditorSet ()
		{
		}

		protected bool CheckIsValid()
		{
			if (editor == null)
				throw new InvalidOperationException ("Session not yet started.");
			if (StartOffset  > editor.CaretOffset || EndOffset < editor.CaretOffset) {
				editor.EndSession ();
				return false;
			}
			return true;
		}

		public virtual void BeforeType(char ch, out bool handledCommand)
		{
			handledCommand = false;
		}

		public virtual void AfterType(char ch)
		{
		}

		public virtual void BeforeReturn(out bool handledCommand) 
		{
			handledCommand = false;
		}

		public virtual void AfterReturn()
		{
		}

		public virtual void BeforeBackspace(out bool handledCommand) 
		{
			handledCommand = false;
		}

		public virtual void AfterBackspace()
		{
		}

		//public virtual void BeforeTab(out bool handledCommand) 
		//{
		//	handledCommand = false;
		//}

		//public virtual void AfterTab()
		//{
		//}

		public virtual void BeforeDelete(out bool handledCommand) 
		{
			handledCommand = false;
		}

		public virtual void AfterDelete()
		{
		}

		public virtual void SessionStarted ()
		{
		}

		public virtual void Dispose ()
		{
		}

	}

	abstract class PairInsertEditSession : EditSession
	{
		IGenericTextSegmentMarker marker;

		protected override void OnEditorSet ()
		{
			startOffset = Editor.CaretOffset - 1;
			endOffset = startOffset + 1;
		}

		public override void SessionStarted ()
		{
			var theme = Editor.Options.GetEditorTheme ();
			var color = SyntaxHighlightingService.GetColor (theme, EditorThemeColors.Selection);
			marker = TextMarkerFactory.CreateGenericTextSegmentMarker (Editor, TextSegmentMarkerEffect.Underline, color, endOffset, 1);
			Editor.AddMarker (marker);
		}

		public override void BeforeBackspace (out bool handledCommand)
		{
			base.BeforeBackspace (out handledCommand);
			if (Editor.CaretOffset > EndOffset) {
				Editor.EndSession ();
				return;
			}
			if (Editor.CaretOffset <= StartOffset + 1) {
				if (HasNoForwardTyping () && StartOffset + 1 < Editor.Length)
					Editor.RemoveText (StartOffset + 1, EndOffset - StartOffset);
				Editor.EndSession ();
			}
		}

		public override void BeforeDelete (out bool handledCommand)
		{
			base.BeforeDelete (out handledCommand);
			if (Editor.CaretOffset <= StartOffset || Editor.CaretOffset >= EndOffset) {
				Editor.EndSession ();
			}
		}

		bool HasNoForwardTyping ()
		{
			for (int i = StartOffset + 1; i < EndOffset - 1; i++) {
				char ch = Editor.GetCharAt (i);
				if (ch != ' ' && ch != '\t') {
					return false;
				}
			}
			return true;
		}

		public override void Dispose ()
		{
			if (marker != null) {
				Editor.RemoveMarker (marker);
				marker = null;
			}
			base.Dispose ();
		}
	}

	/// <summary>
	/// Reassembles the old skip char system - shouldn't be used by new features anymore.
	/// </summary>
	class SkipCharSession : PairInsertEditSession
	{
		readonly char ch;

		public SkipCharSession (char ch)
		{
			this.ch = ch;
		}

		public override void BeforeType (char ch, out bool handledCommand)
		{
			if (CheckIsValid() && ch == this.ch) {
				Editor.CaretOffset++;
				handledCommand = true;
				Editor.EndSession ();
				return;
			}
			handledCommand = false;
		}
	}
}