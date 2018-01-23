//
// SourceEditorView.IEditorOperations.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	partial class SourceEditorView : IMonoDevelopEditorOperations
	{
		bool IEditorOperations.CanPaste => this.EnablePaste;

		bool IEditorOperations.CanDelete => this.EnableDelete;

		bool IEditorOperations.CanCut => this.EnableCut;

		public ITextView TextView { get; set; }

		IEditorOptions IEditorOperations.Options => throw new NotImplementedException ();

		ITrackingSpan IEditorOperations.ProvisionalCompositionSpan => throw new NotImplementedException ();

		string IEditorOperations.SelectedText => TextEditor.SelectedText;

		void IEditorOperations.MoveLineDown (bool extendSelection)
		{
			if (extendSelection) {
				SelectionActions.MoveDown (TextEditor.GetTextEditorData ());
			} else {
				CaretMoveActions.Down (TextEditor.GetTextEditorData ());
			}
		}

		void IEditorOperations.MoveLineUp (bool extendSelection)
		{
			if (extendSelection) {
				SelectionActions.MoveUp (TextEditor.GetTextEditorData ());
			} else {
				CaretMoveActions.Up (TextEditor.GetTextEditorData ());
			}
		}

		void IEditorOperations.MoveToPreviousCharacter (bool extendSelection)
		{
			if (extendSelection) {
				SelectionActions.MoveLeft (TextEditor.GetTextEditorData ());
			} else {
				CaretMoveActions.Left (TextEditor.GetTextEditorData ());
			}
		}

		void IEditorOperations.MoveToNextCharacter (bool extendSelection)
		{
			if (extendSelection) {
				SelectionActions.MoveLeft (TextEditor.GetTextEditorData ());
			} else {
				CaretMoveActions.Left (TextEditor.GetTextEditorData ());
			}
		}

		void IEditorOperations.MoveToEndOfLine (bool extendSelection)
		{
			if (extendSelection) {
				SelectionActions.MoveLineEnd (TextEditor.GetTextEditorData ());
			} else {
				CaretMoveActions.LineEnd (TextEditor.GetTextEditorData ());
			}
		}

		void IEditorOperations.MoveToStartOfLine (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MoveLineHome);
			} else {
				TextEditor.RunAction (CaretMoveActions.LineHome);
			}
		}

		void IEditorOperations.MoveToStartOfDocument (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MoveToDocumentStart);
			} else {
				TextEditor.RunAction (CaretMoveActions.ToDocumentStart);
			}
		}

		void IEditorOperations.MoveToEndOfDocument (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MoveToDocumentEnd);
			} else {
				TextEditor.RunAction (CaretMoveActions.ToDocumentEnd);
			}
		}

		bool IEditorOperations.Backspace ()
		{
			TextEditor.RunAction (DeleteActions.Backspace);
			return true;
		}

		bool IEditorOperations.CopySelection ()
		{
			TextEditor.RunAction (ClipboardActions.Copy);
			return true;
		}

		bool IEditorOperations.CutSelection ()
		{
			ClipboardActions.Cut (TextEditor.GetTextEditorData ());
			return true;
		}

		bool IEditorOperations.Paste ()
		{
			return ClipboardActions.Paste (TextEditor.GetTextEditorData ());
		}

		bool IEditorOperations.InsertNewLine ()
		{
			TextEditor.RunAction (MiscActions.InsertNewLine);
			return true;
		}

		bool IEditorOperations.Tabify ()
		{
			TextEditor.RunAction (MiscActions.InsertTab);
			return true;
		}

		bool IEditorOperations.Untabify ()
		{
			TextEditor.RunAction (MiscActions.RemoveTab);
			return true;
		}

		bool IEditorOperations.DeleteWordToLeft ()
		{
			TextEditor.RunAction (DeleteActions.PreviousWord);
			return true;
		}

		bool IEditorOperations.DeleteWordToRight ()
		{
			TextEditor.RunAction (DeleteActions.NextWord);
			return true;
		}

		void IEditorOperations.ScrollLineCenter ()
		{
			TextEditor.RunAction (MiscActions.RecenterEditor);
		}


		void IEditorOperations.MoveToNextWord (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MoveNextWord);
			} else {
				TextEditor.RunAction (CaretMoveActions.NextWord);
			}
		}

		void IEditorOperations.MoveToPreviousWord (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MovePreviousWord);
			} else {
				TextEditor.RunAction (CaretMoveActions.PreviousWord);
			}
		}

		void IEditorOperations.PageUp (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MovePageUp);
			} else {
				TextEditor.RunAction (CaretMoveActions.PageUp);
			}
		}

		void IEditorOperations.PageDown (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MovePageDown);
			} else {
				TextEditor.RunAction (CaretMoveActions.PageDown);
			}
		}

		bool IEditorOperations.DeleteFullLine ()
		{
			TextEditor.RunAction (DeleteActions.CaretLine);
			return true;
		}

		bool IEditorOperations.DeleteToEndOfLine ()
		{
			TextEditor.RunAction (DeleteActions.CaretLineToEnd);
			return true;
		}

		void IEditorOperations.ScrollLineTop ()
		{
			TextEditor.RunAction (ScrollActions.Up);
		}

		void IEditorOperations.ScrollLineBottom ()
		{
			TextEditor.RunAction (ScrollActions.Down);
		}

		void IEditorOperations.ScrollPageUp ()
		{
			TextEditor.RunAction (ScrollActions.PageUp);
		}

		void IEditorOperations.ScrollPageDown ()
		{
			TextEditor.RunAction (ScrollActions.PageDown);
		}

		bool IEditorOperations.Indent ()
		{
			if (widget.TextEditor.IsSomethingSelected) {
				MiscActions.IndentSelection (widget.TextEditor.GetTextEditorData ());
			} else {
				int offset = widget.TextEditor.LocationToOffset (widget.TextEditor.Caret.Line, 1);
				widget.TextEditor.Insert (offset, widget.TextEditor.Options.IndentationString);
			}
			return true;
		}

		bool IEditorOperations.Unindent ()
		{
			MiscActions.RemoveTab (widget.TextEditor.GetTextEditorData ());
			return true;
		}

		void IEditorOperations.SelectAndMoveCaret (VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectAndMoveCaret (VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectAndMoveCaret (VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveToHome (bool extendSelection)
		{
			if (extendSelection) {
				TextEditor.RunAction (SelectionActions.MoveLineHome);
			} else {
				TextEditor.RunAction (CaretMoveActions.LineHome);
			}
		}

		void IEditorOperations.GotoLine (int lineNumber)
		{
			TextEditor.Caret.Line = lineNumber;
			TextEditor.ScrollToCaret ();
		}

		void IEditorOperations.MoveCurrentLineToTop ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveCurrentLineToBottom ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveToStartOfLineAfterWhiteSpace (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveToStartOfNextLineAfterWhiteSpace (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveToStartOfPreviousLineAfterWhiteSpace (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveToLastNonWhiteSpaceCharacter (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveToTopOfView (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveToBottomOfView (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SwapCaretAndAnchor ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.DeleteToBeginningOfLine ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.DeleteBlankLines ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.DeleteHorizontalWhiteSpace ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.OpenLineAbove ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.OpenLineBelow ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.IncreaseLineIndent ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.DecreaseLineIndent ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.InsertText (string text)
		{
			TextEditor.InsertAtCaret (text);
			return true;
		}

		bool IEditorOperations.InsertTextAsBox (string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd)
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.InsertProvisionalText (string text)
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.Delete ()
		{
			TextEditor.RunAction (DeleteActions.Delete);
			return true;
		}

		bool IEditorOperations.ReplaceSelection (string text)
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.TransposeCharacter ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.TransposeLine ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.TransposeWord ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.MakeLowercase ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.MakeUppercase ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.ToggleCase ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.Capitalize ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.ReplaceText (Span replaceSpan, string text)
		{
			throw new NotImplementedException ();
		}

		int IEditorOperations.ReplaceAllMatches (string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions)
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.InsertFile (string filePath)
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.ConvertSpacesToTabs ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.ConvertTabsToSpaces ()
		{
			throw new NotImplementedException ();
		}

		bool IEditorOperations.NormalizeLineEndings (string replacement)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectCurrentWord ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectEnclosing ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectFirstChild ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectNextSibling (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectPreviousSibling (bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectLine (ITextViewLine viewLine, bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.SelectAll ()
		{
			TextEditor.RunAction (SelectionActions.SelectAll);
		}

		void IEditorOperations.ExtendSelection (int newEnd)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.MoveCaret (ITextViewLine textLine, double horizontalOffset, bool extendSelection)
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.ResetSelection ()
		{
			TextEditor.RunAction (SelectionActions.ClearSelection);
		}

		bool IEditorOperations.CutFullLine ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.ScrollUpAndMoveCaretIfNecessary ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.ScrollDownAndMoveCaretIfNecessary ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.ScrollColumnLeft ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.ScrollColumnRight ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.AddBeforeTextBufferChangePrimitive ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.AddAfterTextBufferChangePrimitive ()
		{
			throw new NotImplementedException ();
		}

		void IEditorOperations.ZoomIn ()
		{
			TextEditor.Options.ZoomIn ();
		}

		void IEditorOperations.ZoomOut ()
		{
			TextEditor.Options.ZoomOut ();
		}

		void IEditorOperations.ZoomTo (double zoomLevel)
		{
			TextEditor.Options.Zoom = zoomLevel;
		}

		string IEditorOperations.GetWhitespaceForVirtualSpace (VirtualSnapshotPoint point)
		{
			throw new NotImplementedException ();
		}

		#region IMonoDevelopEditorOperations members
		void IMonoDevelopEditorOperations.SwitchCaretMode ()
		{
			TextEditor.RunAction (MiscActions.SwitchCaretMode);
		}

		void IMonoDevelopEditorOperations.DeletePreviousSubword ()
		{
			TextEditor.RunAction (DeleteActions.PreviousSubword);
		}

		void IMonoDevelopEditorOperations.DeleteNextSubword ()
		{
			TextEditor.RunAction (DeleteActions.NextSubword);
		}

		void IMonoDevelopEditorOperations.StartCaretPulseAnimation ()
		{
			TextEditor.StartCaretPulseAnimation ();
		}

		void IMonoDevelopEditorOperations.JoinLines ()
		{
			using (var undo = Document.OpenUndoGroup ()) {
				TextEditor.RunAction (Mono.TextEditor.Vi.ViActions.Join);
			}
		}

		void IMonoDevelopEditorOperations.MoveToNextSubWord ()
		{
			TextEditor.RunAction (SelectionActions.MoveNextSubword);
		}

		void IMonoDevelopEditorOperations.MoveToPrevSubWord ()
		{
			TextEditor.RunAction (SelectionActions.MovePreviousSubword);
		}

		void IMonoDevelopEditorOperations.ShowQuickInfo ()
		{
			widget.TextEditor.TextArea.ShowQuickInfo ();
		}

		void IMonoDevelopEditorOperations.MoveBlockUp ()
		{
			using (var undo = TextEditor.OpenUndoGroup ()) {
				TextEditor.RunAction (MiscActions.MoveBlockUp);
				CorrectIndenting ();
			}
		}

		void IMonoDevelopEditorOperations.MoveBlockDown ()
		{
			using (var undo = TextEditor.OpenUndoGroup ()) {
				TextEditor.RunAction (MiscActions.MoveBlockDown);
				CorrectIndenting ();
			}
		}

		void IMonoDevelopEditorOperations.ToggleBlockSelectionMode ()
		{
			TextEditor.SelectionMode = TextEditor.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Normal ? MonoDevelop.Ide.Editor.SelectionMode.Block : MonoDevelop.Ide.Editor.SelectionMode.Normal;
			TextEditor.QueueDraw ();
		}
  		#endregion
	}
}