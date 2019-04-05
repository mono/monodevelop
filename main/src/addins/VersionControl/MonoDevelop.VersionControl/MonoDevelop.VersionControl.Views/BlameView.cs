//
// BlameView.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui.Documents;
using System;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.VersionControl.Views
{
	public interface IBlameView
	{
	}

	internal class BlameView : DocumentController, IBlameView
	{
		BlameWidget widget;
		VersionControlDocumentInfo info;

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			if (widget == null)
				widget = new BlameWidget (info);
			return widget;
		}

		public BlameView (VersionControlDocumentInfo info)
		{
			this.info = info;
		}

		#region IAttachableViewContent implementation
		protected internal override void OnFocused ()
		{
			info.Start ();
			widget.Reset ();

			var textView = info.Controller.GetContent<ITextView> ();
			if (textView != null) {
				var (line, column) = textView.Caret.Position.BufferPosition.GetLineAndColumn1Based ();
				widget.Editor.SetCaretTo (line, column, highlight: false, centerCaret: false);
			}

			if (widget.Allocation.Height == 1 && widget.Allocation.Width == 1) {
				widget.SizeAllocated += HandleComparisonWidgetSizeAllocated;
			} else {
				HandleComparisonWidgetSizeAllocated (null, new Gtk.SizeAllocatedArgs ());
			}
		}

		void HandleComparisonWidgetSizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			widget.Editor.SizeAllocated -= HandleComparisonWidgetSizeAllocated;
			var textView = info.Controller.GetContent<ITextView> ();
			if (textView != null) {
				int firstLineNumber = textView.TextViewLines.FirstVisibleLine.Start.GetContainingLine ().LineNumber;
				widget.Editor.VAdjustment.Value = widget.Editor.LineToY (firstLineNumber + 1);
				widget.Editor.GrabFocus ();
			}
		}

		protected override void OnUnfocused ()
		{
			var textView = info.Controller.GetContent<ITextView> ();
			if (textView != null) {
				var pos = widget.Editor.Caret.Offset;
				var snapshot = textView.TextSnapshot;
				var point = new SnapshotPoint (snapshot, Math.Max (0, Math.Min (snapshot.Length - 1, pos)));
				textView.Caret.MoveTo (point);

				int line = GetLineInCenter (widget.Editor);
				line = Math.Min (line, snapshot.LineCount);
				var middleLine = snapshot.GetLineFromLineNumber (line);
				textView.ViewScroller.EnsureSpanVisible (new SnapshotSpan (textView.TextSnapshot, middleLine.Start, 0), EnsureSpanVisibleOptions.AlwaysCenter);
			}
		}

		int GetLineInCenter (MonoTextEditor editor)
		{
			double midY = editor.VAdjustment.Value + editor.Allocation.Height / 2;
			return editor.YToLine (midY);
		}

		#endregion

		#region IClipboardHandler implementation

		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo info)
		{
			info.Enabled = this.widget.Editor.IsSomethingSelected;
		}

		[CommandHandler (EditCommands.Copy)]
		protected void Copy ()
		{
			this.widget.Editor.RunAction (ClipboardActions.Copy);
		}

		[CommandHandler (EditCommands.SelectAll)]
		protected void SelectAll ()
		{
			this.widget.Editor.RunAction (SelectionActions.SelectAll);
		}

		#endregion
	}
}
