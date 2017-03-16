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

namespace MonoDevelop.VersionControl.Views
{
	public interface IDiffView
	{
	}
	
	class DiffView : BaseView, IDiffView, IUndoHandler, IClipboardHandler
	{
		DiffWidget widget;

		public override Control Control { 
			get {
				if (widget == null) {
					widget = new DiffWidget (info);
					
					ComparisonWidget.DiffEditor.Document.Text = info.Item.Repository.GetBaseText (info.Item.Path);
					ComparisonWidget.SetLocal (ComparisonWidget.OriginalEditor.GetTextEditorData ());
					widget.ShowAll ();
					widget.SetToolbar (WorkbenchWindow.GetToolbar (this));
				}
				return widget;
			}
		}
		
		public ComparisonWidget ComparisonWidget {
			get {
				return this.widget.ComparisonWidget;
			}
		}
		
		public List<Mono.TextEditor.Utils.Hunk> Diff {
			get {
				return ComparisonWidget.Diff;
			}
		}

		VersionControlDocumentInfo info;
		public DiffView (VersionControlDocumentInfo info) : base (GettextCatalog.GetString ("Changes"), GettextCatalog.GetString ("Shows the differences in the code between the current code and the version in the repository"))
		{
			this.info = info;
		}
		
		public DiffView (VersionControlDocumentInfo info, Revision baseRev, Revision toRev) : base (GettextCatalog.GetString ("Changes"))
		{
			this.info = info;
			widget = new DiffWidget (info);
			ComparisonWidget.SetRevision (ComparisonWidget.DiffEditor, baseRev);
			ComparisonWidget.SetRevision (ComparisonWidget.OriginalEditor, toRev);
			
			widget.ShowAll ();
		}
		
		#region IAttachableViewContent implementation

		public int GetLineInCenter (Mono.TextEditor.MonoTextEditor editor)
		{
			double midY = editor.VAdjustment.Value + editor.Allocation.Height / 2;
			return editor.YToLine (midY);
		}
		
		protected override void OnSelected ()
		{
			info.Start ();
			ComparisonWidget.UpdateLocalText ();
			var buffer = info.Document.GetContent<MonoDevelop.Ide.Editor.TextEditor> ();
			if (buffer != null) {
				var loc = buffer.CaretLocation;
				int line = loc.Line < 1 ? 1 : loc.Line;
				int column = loc.Column < 1 ? 1 : loc.Column;
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
			var sourceEditorView = info.Document.GetContent<MonoDevelop.SourceEditor.SourceEditorView> ();
			if (sourceEditorView != null) {
				int line = GetLineInCenter (sourceEditorView.TextEditor);
				ComparisonWidget.OriginalEditor.CenterTo (line, 1);
				ComparisonWidget.OriginalEditor.GrabFocus ();
			}
		}
		
		protected override void OnDeselected ()
		{
			var sourceEditor = info.Document.GetContent <MonoDevelop.SourceEditor.SourceEditorView> ();
			if (sourceEditor != null) {
				sourceEditor.TextEditor.Caret.Location = ComparisonWidget.OriginalEditor.Caret.Location;
				
				int line = GetLineInCenter (ComparisonWidget.OriginalEditor);
				if (Math.Abs (GetLineInCenter (sourceEditor.TextEditor) - line) > 2)
					sourceEditor.TextEditor.CenterTo (line, 1);
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