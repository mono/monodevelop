// 
// VersionControlView.cs
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
using System;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.VersionControl.Views
{
	public interface IDiffView
	{
	}
	
	class DiffView : DocumentController, IDiffView, IUndoHandler, IClipboardHandler
	{
		DiffWidget widget;

		void CreateWiget ()
		{
			if (widget == null) {
				widget = new DiffWidget (info);

				try {
					ComparisonWidget.DiffEditor.Document.Text = info.Item.Repository.GetBaseText (info.Item.Path);
				} catch (Exception ex) {
					LoggingService.LogInternalError ("Error fetching text from repository ", ex);
				}
				ComparisonWidget.SetLocal (ComparisonWidget.OriginalEditor.GetTextEditorData ());
				widget.ShowAll ();
			}
		}

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			CreateWiget ();
			widget.SetToolbar (view.GetToolbar ());
			return widget;
		}

		public ComparisonWidget ComparisonWidget {
			get {
				CreateWiget ();
				return this.widget.ComparisonWidget;
			}
		}
		
		public List<Mono.TextEditor.Utils.Hunk> Diff {
			get {
				return ComparisonWidget.Diff;
			}
		}

		VersionControlDocumentInfo info;
		public DiffView (VersionControlDocumentInfo info)
		{
			this.info = info;
		}
		
		#region IAttachableViewContent implementation

		public int GetLineInCenter (Mono.TextEditor.MonoTextEditor editor)
		{
			double midY = editor.VAdjustment.Value + editor.Allocation.Height / 2;
			return editor.YToLine (midY);
		}
		
		protected override void OnFocused ()
		{
			info.Start ();
			if (ComparisonWidget.originalComboBox.Text == GettextCatalog.GetString ("Local"))
				ComparisonWidget.UpdateLocalText ();
			var textView = info.Controller.GetContent<ITextView> ();
			if (textView != null) {
				var (line,column) = textView.Caret.Position.BufferPosition.GetLineAndColumn1Based();
				ComparisonWidget.OriginalEditor.SetCaretTo (line, column);
			}
			
			if (ComparisonWidget.Allocation.Height == 1 && ComparisonWidget.Allocation.Width == 1) {
				ComparisonWidget.SizeAllocated += HandleComparisonWidgetSizeAllocated;
			} else {
				HandleComparisonWidgetSizeAllocated (null, new Gtk.SizeAllocatedArgs ());
			}
			
			widget.UpdatePatchView ();
		}

		void HandleComparisonWidgetSizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			ComparisonWidget.SizeAllocated -= HandleComparisonWidgetSizeAllocated;
			var textView = info.Controller.GetContent<ITextView> ();
			if (textView != null) {
				int firstLineNumber = textView.TextViewLines.FirstVisibleLine.Start.GetContainingLine ().LineNumber;
				ComparisonWidget.OriginalEditor.VAdjustment.Value = ComparisonWidget.OriginalEditor.LineToY (firstLineNumber + 1);
				ComparisonWidget.OriginalEditor.GrabFocus ();
			}
		}
		
		protected override void OnUnfocused ()
		{
			var textView = info.Controller.GetContent <ITextView> ();
			if (textView != null) {
				var pos = ComparisonWidget.OriginalEditor.Caret.Offset;
				var snapshot = textView.TextSnapshot;
				var point = new SnapshotPoint (snapshot, Math.Max (0, Math.Min (snapshot.Length - 1, pos)));
				textView.Caret.MoveTo (point);

				int line = GetLineInCenter (ComparisonWidget.OriginalEditor);
				line = Math.Min (line, snapshot.LineCount);
				var middleLine = snapshot.GetLineFromLineNumber (line);
				textView.ViewScroller.EnsureSpanVisible (new SnapshotSpan (textView.TextSnapshot, middleLine.Start, 0), EnsureSpanVisibleOptions.AlwaysCenter);
			}
		}

		#endregion
		
		#region IUndoHandler implementation
		void IUndoHandler.Undo ()
		{
			this.ComparisonWidget.OriginalEditor.Document.Undo ();
		}

		void IUndoHandler.Redo ()
		{
			this.ComparisonWidget.OriginalEditor.Document.Redo ();
		}

		IDisposable IUndoHandler.OpenUndoGroup ()
		{
			return this.ComparisonWidget.OriginalEditor.OpenUndoGroup ();
		}

		bool IUndoHandler.EnableUndo {
			get {
				return this.ComparisonWidget.OriginalEditor.Document.CanUndo;
			}
		}

		bool IUndoHandler.EnableRedo {
			get {
				return this.ComparisonWidget.OriginalEditor.Document.CanRedo;
			}
		}
		#endregion

		#region IClipboardHandler implementation
		void IClipboardHandler.Cut ()
		{
			var editor = this.widget.FocusedEditor;
			if (editor == null)
				return;
			editor.RunAction (Mono.TextEditor.ClipboardActions.Cut);
		}

		void IClipboardHandler.Copy ()
		{
			var editor = this.widget.FocusedEditor;
			if (editor == null)
				return;
			editor.RunAction (Mono.TextEditor.ClipboardActions.Copy);
		}

		void IClipboardHandler.Paste ()
		{
			var editor = this.widget.FocusedEditor;
			if (editor == null)
				return;
			editor.RunAction (Mono.TextEditor.ClipboardActions.Paste);
		}

		void IClipboardHandler.Delete ()
		{
			var editor = this.widget.FocusedEditor;
			if (editor == null)
				return;
			if (editor.IsSomethingSelected) {
				editor.DeleteSelectedText ();
			} else {
				editor.RunAction (Mono.TextEditor.DeleteActions.Delete);
			}
		}

		void IClipboardHandler.SelectAll ()
		{
			var editor = this.widget.FocusedEditor;
			if (editor == null)
				return;
			editor.RunAction (Mono.TextEditor.SelectionActions.SelectAll);
		}

		bool IClipboardHandler.EnableCut {
			get {
				var editor = this.widget.FocusedEditor;
				if (editor == null)
					return false;
				return editor.IsSomethingSelected && !editor.Document.IsReadOnly;
			}
		}

		bool IClipboardHandler.EnableCopy {
			get {
				var editor = this.widget.FocusedEditor;
				if (editor == null)
					return false;
				return editor.IsSomethingSelected;
			}
		}

		bool IClipboardHandler.EnablePaste {
			get {
				var editor = this.widget.FocusedEditor;
				if (editor == null)
					return false;
				return !editor.Document.IsReadOnly;
			}
		}

		bool IClipboardHandler.EnableDelete {
			get {
				var editor = this.widget.FocusedEditor;
				if (editor == null)
					return false;
				return !editor.Document.IsReadOnly;
			}
		}

		bool IClipboardHandler.EnableSelectAll {
			get {
				return true;
			}
		}
		#endregion
	}
}