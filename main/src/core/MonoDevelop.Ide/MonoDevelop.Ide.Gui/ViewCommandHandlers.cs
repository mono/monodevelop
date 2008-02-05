//
// ViewCommandHandlers.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Gtk;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui
{
	public class ViewCommandHandlers : ICommandRouter
	{
		IWorkbenchWindow window;
		Document doc;

		public ViewCommandHandlers (IWorkbenchWindow window)
		{
			this.window = window;
			doc = IdeApp.Workbench.WrapDocument (window);
		}
		
		public T GetContent <T>()
		{
			return (T) window.ActiveViewContent.GetContent (typeof(T));
		}
		
		object ICommandRouter.GetNextCommandTarget ()
		{
			return doc.ExtendedCommandTargetChain;
		}
		
		[CommandHandler (FileCommands.CloseFile)]
		protected void OnCloseFile ()
		{
			window.CloseWindow (false, true, 0);
		}
		
		[CommandHandler (FileCommands.Save)]
		protected void OnSaveFile ()
		{
			IdeApp.Workbench.FindDocument (window).Save ();
		}
		
		[CommandUpdateHandler (FileCommands.Save)]
		protected void OnUpdateSaveFile (CommandInfo info)
		{
			if (window.ViewContent.IsViewOnly) {
				info.Enabled = false;
				return;
			}
			
			IViewContent content = window.ActiveViewContent as IViewContent;
			if (content != null)
				info.Enabled = !content.IsViewOnly && content.IsDirty;
			else
				info.Enabled = false;
		}

		[CommandHandler (FileCommands.SaveAs)]
		protected void OnSaveFileAs ()
		{
			IdeApp.Workbench.FindDocument (window).SaveAs ();
		}
		
		[CommandUpdateHandler (FileCommands.SaveAs)]
		protected void OnUpdateSaveFileAs (CommandInfo info)
		{
			IViewContent content = window.ActiveViewContent as IViewContent;
			if (content != null && content.IsFile)
				info.Enabled = !content.IsViewOnly;
			else
				info.Enabled = false;
		}
		
		[CommandHandler (FileCommands.ReloadFile)]
		protected void OnReloadFile ()
		{
			if (Services.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure that you want to reload the file?")))
				doc.Reload ();
		}
		
		[CommandUpdateHandler (FileCommands.ReloadFile)]
		protected void OnUpdateReloadFile (CommandInfo info)
		{
			info.Enabled = window.ViewContent.ContentName != null && !window.ViewContent.IsViewOnly;
		}
		
		
		/*** Edit commands ***/
		
		[CommandHandler (EditCommands.Undo)]
		protected void OnUndo ()
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				editable.Undo();
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo info)
		{
			IEditableTextBuffer textBuffer = GetContent <IEditableTextBuffer> ();
			info.Enabled = textBuffer != null && textBuffer.EnableUndo;
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null) {
				editable.Redo();
			}
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo info)
		{
			IEditableTextBuffer textBuffer = GetContent <IEditableTextBuffer> ();
			info.Enabled = textBuffer != null && textBuffer.EnableRedo;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				editable.ClipboardHandler.Cut(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo info)
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				info.Enabled = editable.ClipboardHandler.EnableCut;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				editable.ClipboardHandler.Copy(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo info)
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				info.Enabled = editable.ClipboardHandler.EnableCopy;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				editable.ClipboardHandler.Paste(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo info)
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				info.Enabled = editable.ClipboardHandler.EnablePaste;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				editable.ClipboardHandler.Delete(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo info)
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			info.Enabled = editable != null && editable.ClipboardHandler.EnableDelete;
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAll ()
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				editable.ClipboardHandler.SelectAll(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.SelectAll)]
		protected void OnUpdateSelectAll (CommandInfo info)
		{
			IEditableTextBuffer editable = GetContent <IEditableTextBuffer> ();
			if (editable != null)
				info.Enabled = editable.ClipboardHandler.EnableSelectAll;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.WordCount)]
		protected void OnWordCount()
		{
			WordCountDialog wcd = new WordCountDialog ();
			wcd.Run ();
			wcd.Hide ();
		}
		
		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		protected void OnUpdateCommentCode (CommandInfo info)
		{
			info.Enabled = GetContent<ICodeStyleOperations> () != null;
		}
		
		[CommandHandler (EditCommands.ToggleCodeComment)]
		public void OnUncommentCode()
		{
			ICodeStyleOperations  styling = GetContent <ICodeStyleOperations> ();
			if (styling != null)
				styling.ToggleCodeComment ();
		}
		
		[CommandHandler (EditCommands.IndentSelection)]
		public void OnIndentSelection()
		{
			ICodeStyleOperations  styling = GetContent<ICodeStyleOperations> ();
			if (styling != null)
				styling.IndentSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.IndentSelection)]
		protected void OnUpdateIndentSelection (CommandInfo info)
		{
			info.Enabled = GetContent<ICodeStyleOperations> () != null;
		}
		
		[CommandHandler (EditCommands.UnIndentSelection)]
		public void OnUnIndentSelection()
		{
			ICodeStyleOperations  styling = GetContent<ICodeStyleOperations> ();
			if (styling != null)
				styling.UnIndentSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.UnIndentSelection)]
		protected void OnUpdateUnIndentSelection (CommandInfo info)
		{
			info.Enabled = GetContent<ICodeStyleOperations> () != null;
		}
		
		[CommandHandler (EditCommands.UppercaseSelection)]
		public void OnUppercaseSelection ()
		{
			IEditableTextBuffer buffer = GetContent <IEditableTextBuffer> ();
			if (buffer != null)
			{
				if (buffer.SelectedText == String.Empty)
				{
					int pos = buffer.CursorPosition;
					string ch = buffer.GetText (pos, pos + 1);
					buffer.DeleteText (pos, 1);
					buffer.InsertText (pos, ch.ToUpper ());
					buffer.CursorPosition = pos + 1;
				} else
				{
					string newText = buffer.SelectedText.ToUpper ();
					int startPos = buffer.SelectionStartPosition;
					buffer.DeleteText (startPos, buffer.SelectedText.Length);
					buffer.InsertText (startPos, newText);
				}
			}
		}
		
		[CommandUpdateHandler (SearchCommands.GotoLineNumber)]
		void OnUpdateGotoLineNumber (CommandInfo info)
		{
			info.Enabled = GetContent <IPositionable> () != null && MonoDevelop.Ide.Gui.Dialogs.GotoLineDialog.CanShow;
		}
		
		[CommandHandler (SearchCommands.GotoLineNumber)]
		void OnGotoLineNumber ()
		{
			MonoDevelop.Ide.Gui.Dialogs.GotoLineDialog.ShowDialog (GetContent <IPositionable> ());
		}
		
		[CommandHandler (EditCommands.LowercaseSelection)]
		public void OnLowercaseSelection ()
		{
			IEditableTextBuffer buffer = GetContent <IEditableTextBuffer> ();
			if (buffer != null)
			{
				if (buffer.SelectedText == String.Empty)
				{
					int pos = buffer.CursorPosition;
					string ch = buffer.GetText (pos, pos + 1);
					buffer.DeleteText (pos, 1);
					buffer.InsertText (pos, ch.ToLower ());
					buffer.CursorPosition = pos + 1;
				} else
				{
					string newText = buffer.SelectedText.ToLower ();
					int startPos = buffer.SelectionStartPosition;
					buffer.DeleteText (startPos, buffer.SelectedText.Length);
					buffer.InsertText (startPos, newText);
				}
			}
		}
		
		[CommandUpdateHandler (EditCommands.LowercaseSelection)]
		protected void OnLowercaseSelection (CommandInfo info)
		{
			info.Enabled = GetContent <IEditableTextBuffer> () != null;
		}
		

		// Text editor commands
		
		[CommandUpdateHandler (TextEditorCommands.LineEnd)]
		[CommandUpdateHandler (TextEditorCommands.LineStart)]
		[CommandUpdateHandler (TextEditorCommands.DeleteLeftChar)]
		[CommandUpdateHandler (TextEditorCommands.DeleteRightChar)]
		[CommandUpdateHandler (TextEditorCommands.CharLeft)]
		[CommandUpdateHandler (TextEditorCommands.CharRight)]
		[CommandUpdateHandler (TextEditorCommands.LineUp)]
		[CommandUpdateHandler (TextEditorCommands.LineDown)]
		[CommandUpdateHandler (TextEditorCommands.DocumentStart)]
		[CommandUpdateHandler (TextEditorCommands.DocumentEnd)]
		[CommandUpdateHandler (TextEditorCommands.DeleteLine)]
		[CommandUpdateHandler (TextEditorCommands.DeleteToLineEnd)]
		[CommandUpdateHandler (TextEditorCommands.MoveBlockUp)]
		[CommandUpdateHandler (TextEditorCommands.MoveBlockDown)]		
		[CommandUpdateHandler (TextEditorCommands.GotoMatchingBrace)]		
		protected void OnUpdateLineEnd (CommandInfo info)
		{
			// If the current document is not an editor, just ignore the text
			// editor commands.
			info.Bypass = doc.TextEditor == null;
		}
		
		[CommandHandler (TextEditorCommands.LineEnd)]
		protected void OnLineEnd ()
		{
			doc.TextEditor.CursorColumn = int.MaxValue;
		}
		
		[CommandHandler (TextEditorCommands.LineStart)]
		protected void OnLineStart ()
		{
			doc.TextEditor.CursorColumn = 1;
		}
		
		[CommandHandler (TextEditorCommands.DeleteLeftChar)]
		protected void OnDeleteLeftChar ()
		{
			int pos = doc.TextEditor.CursorPosition;
			if (pos > 0) {
				doc.TextEditor.DeleteText (pos-1, 1);
			}
		}
		
		[CommandHandler (TextEditorCommands.DeleteRightChar)]
		protected void OnDeleteRightChar ()
		{
			int pos = doc.TextEditor.CursorPosition;
			if (pos < doc.TextEditor.TextLength) {
				doc.TextEditor.DeleteText (pos, 1);
			}
		}
		
		[CommandHandler (TextEditorCommands.CharLeft)]
		protected void OnCharLeft ()
		{
			int pos = doc.TextEditor.CursorPosition;
			if (pos > 0) {
				doc.TextEditor.CursorPosition = pos - 1;
			}
		}
		
		[CommandHandler (TextEditorCommands.CharRight)]
		protected void OnCharRight ()
		{
			int pos = doc.TextEditor.CursorPosition;
			if (pos < doc.TextEditor.TextLength) {
				doc.TextEditor.CursorPosition = pos + 1;
			}
		}
		
		[CommandHandler (TextEditorCommands.LineUp)]
		protected void OnLineUp ()
		{
			int lin = doc.TextEditor.CursorLine;
			if (lin > 1) {
				doc.TextEditor.CursorLine = lin - 1;
			}
		}
		
		[CommandHandler (TextEditorCommands.LineDown)]
		protected void OnLineDown ()
		{
			doc.TextEditor.CursorLine++;
		}
		
		[CommandHandler (TextEditorCommands.DocumentStart)]
		protected void OnDocumentStart ()
		{
			doc.TextEditor.CursorPosition = 0;
		}
		
		[CommandHandler (TextEditorCommands.DocumentEnd)]
		protected void OnDocumentEnd ()
		{
			doc.TextEditor.CursorPosition = doc.TextEditor.TextLength;
		}
		
		[CommandHandler (TextEditorCommands.GotoMatchingBrace)]
		protected void OnGotoMatchingBrace ()
		{
			doc.TextEditor.GotoMatchingBrace ();
		}
		
		[CommandHandler (TextEditorCommands.DeleteLine)]
		protected void OnDeleteLine ()
		{
			doc.TextEditor.BeginAtomicUndo ();
			int col = doc.TextEditor.CursorColumn;
			doc.TextEditor.DeleteLine (doc.TextEditor.CursorLine);
			doc.TextEditor.CursorColumn = col;
			doc.TextEditor.EndAtomicUndo ();
		}
		
		[CommandHandler (TextEditorCommands.DeleteToLineEnd)]
		protected void OnDeleteToLineEnd ()
		{
			int len = doc.TextEditor.GetLineLength (doc.TextEditor.CursorLine);
			int col = doc.TextEditor.CursorColumn;
			if (col == len + 1) {
				int npos = doc.TextEditor.GetPositionFromLineColumn (doc.TextEditor.CursorLine + 1, 1);
				doc.TextEditor.DeleteText (doc.TextEditor.CursorPosition, npos - doc.TextEditor.CursorPosition);
			} else {
				doc.TextEditor.DeleteText (doc.TextEditor.CursorPosition, len - col + 1);
			}
		}
		
		[CommandHandler (TextEditorCommands.MoveBlockUp)]
		protected void OnMoveBlockUp ()
		{
			int lineStart = -1, lineEnd = -1, colStart = -1, colEnd = -1;
			doc.TextEditor.GetLineColumnFromPosition (doc.TextEditor.SelectionStartPosition, out lineStart, out colStart);
			doc.TextEditor.GetLineColumnFromPosition (doc.TextEditor.SelectionEndPosition, out lineEnd, out colEnd);
			
			// Full line selection behaves oddly, in that the end position will be reported as
			// the first column of the next line.  We don't want that behavior.
			if(lineStart != lineEnd && colEnd == 1)
				lineEnd--;
			
			if(lineStart == 1)
				return;
			
			// We don't want to do partial lines.  If any part of a line is selected,
			// we move the entire line.

			doc.TextEditor.BeginAtomicUndo ();
			int oldSelStart = doc.TextEditor.SelectionStartPosition;
			int oldSelEnd = doc.TextEditor.SelectionEndPosition;
			
			int startPos = doc.TextEditor.GetPositionFromLineColumn (lineStart, 1);
			int endPos = doc.TextEditor.GetPositionFromLineColumn (lineEnd, doc.TextEditor.GetLineLength(lineEnd) + 1) + 1;  // Include \n
			
			string text = doc.TextEditor.GetText(startPos, endPos);
			doc.TextEditor.DeleteText(startPos, endPos - startPos);
			int newStartPos = doc.TextEditor.GetPositionFromLineColumn(lineStart - 1, 1);
			doc.TextEditor.InsertText(newStartPos, text);
			
			// Now we either reset the selection or the cursor, depending
			// on what the case was when we started.
			int selStart = newStartPos + colStart - 1;
			int selEnd = selStart + (oldSelEnd - oldSelStart);
			doc.TextEditor.Select(selStart, selEnd);

			doc.TextEditor.EndAtomicUndo ();
		}
		
		[CommandHandler (TextEditorCommands.MoveBlockDown)]
		protected void OnMoveBlockDown ()
		{
			int lineStart = -1, lineEnd = -1, colStart = -1, colEnd = -1;
			doc.TextEditor.GetLineColumnFromPosition (doc.TextEditor.SelectionStartPosition, out lineStart, out colStart);
			doc.TextEditor.GetLineColumnFromPosition (doc.TextEditor.SelectionEndPosition, out lineEnd, out colEnd);
			
			// Full line selection behaves oddly, in that the end position will be reported as
			// the first column of the next line.  We don't want that behavior.
			if(lineStart != lineEnd && colEnd == 1)
				lineEnd--;
			
			if(doc.TextEditor.GetPositionFromLineColumn (lineEnd + 1, 1) == -1)
				return;
			
			// We don't want to do partial lines.  If any part of a line is selected,
			// we move the entire line.

			doc.TextEditor.BeginAtomicUndo ();
			int oldSelStart = doc.TextEditor.SelectionStartPosition;
			int oldSelEnd = doc.TextEditor.SelectionEndPosition;

			int startPos = doc.TextEditor.GetPositionFromLineColumn (lineStart, 1);
			int endPos = doc.TextEditor.GetPositionFromLineColumn (lineEnd, doc.TextEditor.GetLineLength(lineEnd) + 1) + 1;  // Include \n

			// If the next line is the last line, don't add 1 for the \n or we'll end up adding an extra line.
			if (doc.TextEditor.GetPositionFromLineColumn (lineEnd + 2, 1) == -1)
				endPos--;
			
			string text = doc.TextEditor.GetText(startPos, endPos);
			doc.TextEditor.DeleteText(startPos, endPos - startPos);
			int newStartPos = doc.TextEditor.GetPositionFromLineColumn(lineStart + 1, 1);
			doc.TextEditor.InsertText(newStartPos, text);
			
			// Now we either reset the selection or the cursor, depending
			// on what the case was when we started.
			int selStart = newStartPos + colStart - 1;
			int selEnd = selStart + (oldSelEnd - oldSelStart);
			doc.TextEditor.Select(selStart, selEnd);
			doc.TextEditor.EndAtomicUndo ();
		}
#region Bookmarks
		[CommandUpdateHandler (SearchCommands.ToggleBookmark)]
		[CommandUpdateHandler (SearchCommands.PrevBookmark)]
		[CommandUpdateHandler (SearchCommands.NextBookmark)]
		[CommandUpdateHandler (SearchCommands.ClearBookmarks)]
		protected void UpdateBookmarkCommands (CommandInfo info)
		{
			info.Enabled = GetContent <IBookmarkBuffer> () != null;
		}
		
		[CommandHandler (SearchCommands.ToggleBookmark)]
		public void ToggleBookmark ()
		{
			IBookmarkBuffer markBuffer = GetContent <IBookmarkBuffer> ();
			Debug.Assert (markBuffer != null);
			int position = markBuffer.CursorPosition;
			markBuffer.SetBookmarked (position, !markBuffer.IsBookmarked (position));
		}
		
		[CommandHandler (SearchCommands.PrevBookmark)]
		public void PrevBookmark ()
		{
			IBookmarkBuffer markBuffer = GetContent <IBookmarkBuffer> ();
			Debug.Assert (markBuffer != null);
			markBuffer.PrevBookmark ();
		}
		
		[CommandHandler (SearchCommands.NextBookmark)]
		public void NextBookmark ()
		{
			IBookmarkBuffer markBuffer = GetContent <IBookmarkBuffer> ();
			Debug.Assert (markBuffer != null);
			markBuffer.NextBookmark ();
		}
		
		[CommandHandler (SearchCommands.ClearBookmarks)]
		public void ClearBookmarks ()
		{
			IBookmarkBuffer markBuffer = GetContent <IBookmarkBuffer> ();
			Debug.Assert (markBuffer != null);
			markBuffer.ClearBookmarks ();
		}
#endregion
		
#region Splitting
		[CommandUpdateHandler (WindowCommands.SplitWindowHorizontally)]
		protected void UpdateSplitWindowHorizontally (CommandInfo info)
		{
			ISplittable split = GetContent <ISplittable> ();
			info.Enabled = split != null && split.EnableSplitHorizontally;
		}
		[CommandHandler (WindowCommands.SplitWindowHorizontally)]
		public void SplitWindowHorizontally ()
		{
			ISplittable split = GetContent <ISplittable> ();
			Debug.Assert (split != null);
			split.SplitHorizontally ();
		}

		[CommandUpdateHandler (WindowCommands.SplitWindowVertically)]
		protected void UpdateSplitWindowVertically (CommandInfo info)
		{
			ISplittable split = GetContent <ISplittable> ();
			info.Enabled = split != null && split.EnableSplitVertically;
		}
		[CommandHandler (WindowCommands.SplitWindowVertically)]
		public void SplitWindowVertically ()
		{
			ISplittable split = GetContent <ISplittable> ();
			Debug.Assert (split != null);
			split.SplitVertically ();
		}

		[CommandUpdateHandler (WindowCommands.UnsplitWindow)]
		protected void UpdateUnsplitWindow (CommandInfo info)
		{
			ISplittable split = GetContent <ISplittable> ();
			info.Enabled = split != null && split.EnableUnsplit;
		}
		[CommandHandler (WindowCommands.UnsplitWindow)]
		public void UnsplitWindow ()
		{
			ISplittable split = GetContent <ISplittable> ();
			Debug.Assert (split != null);
			split.Unsplit ();
		}
#endregion
#region Printing
		[CommandUpdateHandler (FileCommands.PrintDocument)]
		[CommandUpdateHandler (FileCommands.PrintPreviewDocument)]
		protected void UpdatePrintCommands (CommandInfo info)
		{
			info.Enabled = GetContent <IPrintable> () != null;
		}
		[CommandHandler (FileCommands.PrintDocument)]
		public void PrintDocument ()
		{
			GetContent <IPrintable> ().PrintDocument ();
		}
		[CommandHandler (FileCommands.PrintPreviewDocument)]
		public void PrintPreviewDocument ()
		{
			GetContent <IPrintable> ().PrintPreviewDocument ();
		}
#endregion
		
	}
}
