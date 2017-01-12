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
using System.Diagnostics;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.Gui
{
	public class ViewCommandHandlers
	{
		IWorkbenchWindow window;
		Document doc;

		public ViewCommandHandlers (IWorkbenchWindow window)
		{
			this.window = window;
			doc = IdeApp.Workbench.WrapDocument (window);
		}
		
		public T GetContent <T>() where T : class
		{
			return (T) window.ActiveViewContent.GetContent (typeof(T));
		}
		
		[CommandHandler (FileCommands.Save)]
		protected void OnSaveFile ()
		{
			doc.Save ();
		}
		
		[CommandUpdateHandler (FileCommands.Save)]
		protected void OnUpdateSaveFile (CommandInfo info)
		{
			info.Enabled = doc.IsDirty;
		}

		[CommandHandler (FileCommands.SaveAs)]
		protected void OnSaveFileAs ()
		{
			doc.SaveAs ();
		}
		
		[CommandUpdateHandler (FileCommands.SaveAs)]
		protected void OnUpdateSaveFileAs (CommandInfo info)
		{
			info.Enabled = doc.IsFile && !doc.IsViewOnly;
		}
		
		[CommandHandler (FileCommands.ReloadFile)]
		protected void OnReloadFile ()
		{
			if (MessageService.GenericAlert (MonoDevelop.Ide.Gui.Stock.Warning,
			                                GettextCatalog.GetString ("Revert unsaved changes to document '{0}'?", Path.GetFileName (doc.Name)),
			                                GettextCatalog.GetString ("All changes made to the document will be permanently lost."), 0, AlertButton.Cancel, AlertButton.Revert) == AlertButton.Revert)
				doc.Reload ();
		}
		
		[CommandUpdateHandler (FileCommands.ReloadFile)]
		protected void OnUpdateReloadFile (CommandInfo info)
		{
			info.Enabled = window.ViewContent.ContentName != null && !window.ViewContent.IsViewOnly && window.Document != null && window.Document.IsDirty;
		}

		[CommandHandler (FileCommands.OpenContainingFolder)]
		protected void OnOpenFolder ()
		{
			// A tab will always hold a file, never a folder.
			FilePath path = Path.GetDirectoryName (doc.FileName);
			DesktopService.OpenFolder (path, doc.FileName);
		}
		
		[CommandUpdateHandler (FileCommands.OpenContainingFolder)]
		protected void OnUpdateOpenFolder (CommandInfo info)
		{
			info.Visible = doc != null && !doc.FileName.IsNullOrEmpty;
			info.Enabled = info.Visible;
		}

		
		/*** Edit commands ***/
		
		[CommandHandler (EditCommands.Undo)]
		protected void OnUndo ()
		{
			IUndoHandler editable = GetContent <IUndoHandler> ();
			if (editable != null)
				editable.Undo();
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo info)
		{
			IUndoHandler textBuffer = GetContent <IUndoHandler> ();
			info.Enabled = textBuffer != null && textBuffer.EnableUndo;
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
			IUndoHandler editable = GetContent <IUndoHandler> ();
			if (editable != null) {
				editable.Redo();
			}
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo info)
		{
			IUndoHandler textBuffer = GetContent <IUndoHandler> ();
			info.Enabled = textBuffer != null && textBuffer.EnableRedo;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (handler != null)
				handler.Cut ();
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo info)
		{
			bool inWpf = false;
			#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
			#endif
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (!inWpf && handler != null && handler.EnableCut)
				info.Enabled = true;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (handler != null)
				handler.Copy ();
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo info)
		{
			bool inWpf = false;
			#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
			#endif
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (!inWpf && handler != null && handler.EnableCopy)
				info.Enabled = true;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (handler != null)
				handler.Paste ();
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo info)
		{
			bool inWpf = false;
			#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
			#endif
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (!inWpf && handler != null && handler.EnablePaste)
				info.Enabled = true;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (handler != null)
				handler.Delete ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo info)
		{
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (handler != null)
				info.Enabled = handler.EnableDelete;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAll ()
		{
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (handler != null)
				handler.SelectAll ();
		}
		
		[CommandUpdateHandler (EditCommands.SelectAll)]
		protected void OnUpdateSelectAll (CommandInfo info)
		{
			bool inWpf = false;
			#if WIN32
			if (System.Windows.Input.Keyboard.FocusedElement != null)
				inWpf = true;
			#endif
			IClipboardHandler handler = GetContent <IClipboardHandler> ();
			if (!inWpf && handler != null)
				info.Enabled = handler.EnableSelectAll;
			else
				info.Bypass = true;
		}
		
		[CommandHandler (EditCommands.UppercaseSelection)]
		public void OnUppercaseSelection ()
		{
			var buffer = GetContent <TextEditor> ();
			if (buffer == null)
				return;
			
			string selectedText = buffer.SelectedText;
			if (string.IsNullOrEmpty (selectedText)) {
				int pos = buffer.CaretOffset;
				string ch = buffer.GetTextAt (pos, pos + 1);
				string upper = ch.ToUpper ();
				if (upper == ch) {
					buffer.CaretOffset = pos + 1;
					return;
				}
				using (var undo = buffer.OpenUndoGroup ()) {
					buffer.RemoveText (pos, 1);
					buffer.InsertText (pos, upper);
					buffer.CaretOffset = pos + 1;
				}
			} else {
				string newText = selectedText.ToUpper ();
				if (newText == selectedText)
					return;
				int startPos = buffer.SelectionRange.Offset;
				using (var undo = buffer.OpenUndoGroup ()) {
					buffer.RemoveText (startPos, selectedText.Length);
					buffer.InsertText (startPos, newText);
					buffer.SetSelection (startPos, startPos + newText.Length);
				}
			}
		}
		
		[CommandUpdateHandler (EditCommands.UppercaseSelection)]
		protected void OnUppercaseSelection (CommandInfo info)
		{
			var buffer = GetContent <TextEditor> ();
			info.Enabled = buffer != null;
		}
		
		[CommandHandler (EditCommands.LowercaseSelection)]
		public void OnLowercaseSelection ()
		{
			var buffer = GetContent <TextEditor> ();
			if (buffer == null)
				return;
			
			string selectedText = buffer.SelectedText;
			if (string.IsNullOrEmpty (selectedText)) {
				int pos = buffer.CaretOffset;
				string ch = buffer.GetTextAt (pos, pos + 1);
				string lower = ch.ToLower ();
				if (lower == ch) {
					buffer.CaretOffset = pos + 1;
					return;
				};
				using (var undo = buffer.OpenUndoGroup ()) {
					buffer.RemoveText (pos, 1);
					buffer.InsertText (pos, lower);
					buffer.CaretOffset = pos + 1;
				}
			} else {
				string newText = selectedText.ToLower ();
				if (newText == selectedText)
					return;
				int startPos = buffer.SelectionRange.Offset;
				using (var undo = buffer.OpenUndoGroup ()) {
					buffer.RemoveText (startPos, selectedText.Length);
					buffer.InsertText (startPos, newText);
					buffer.SetSelection (startPos, startPos + newText.Length);
				}
			}
		}
		
		[CommandUpdateHandler (EditCommands.LowercaseSelection)]
		protected void OnLowercaseSelection (CommandInfo info)
		{
			var buffer = GetContent <TextEditor> ();
			info.Enabled = buffer != null;
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
		[CommandUpdateHandler (TextEditorCommands.MoveBlockUp)]
		[CommandUpdateHandler (TextEditorCommands.MoveBlockDown)]		
		[CommandUpdateHandler (TextEditorCommands.GotoMatchingBrace)]		
		protected void OnUpdateLineEnd (CommandInfo info)
		{
			// If the current document is not an editor, just ignore the text
			// editor commands.
			info.Bypass = doc.Editor?.HasFocus == false;
		}
		
		[CommandHandler (TextEditorCommands.LineEnd)]
		protected void OnLineEnd ()
		{
			doc.Editor.EditorActionHost.MoveCaretToLineEnd ();
		}
		
		[CommandHandler (TextEditorCommands.LineStart)]
		protected void OnLineStart ()
		{
			doc.Editor.EditorActionHost.MoveCaretToLineStart ();
		}
		
		[CommandHandler (TextEditorCommands.DeleteLeftChar)]
		protected void OnDeleteLeftChar ()
		{
			doc.Editor.EditorActionHost.Backspace ();
		}
		
		[CommandHandler (TextEditorCommands.DeleteRightChar)]
		protected void OnDeleteRightChar ()
		{
			doc.Editor.EditorActionHost.Delete ();
		}
		
		[CommandHandler (TextEditorCommands.CharLeft)]
		protected void OnCharLeft ()
		{
			doc.Editor.EditorActionHost.MoveCaretLeft ();
		}
		
		[CommandHandler (TextEditorCommands.CharRight)]
		protected void OnCharRight ()
		{
			doc.Editor.EditorActionHost.MoveCaretRight ();
		}
		
		[CommandHandler (TextEditorCommands.LineUp)]
		protected void OnLineUp ()
		{
			doc.Editor.EditorActionHost.MoveCaretUp ();
		}
		
		[CommandHandler (TextEditorCommands.LineDown)]
		protected void OnLineDown ()
		{
			doc.Editor.EditorActionHost.MoveCaretDown ();
		}
		
		[CommandHandler (TextEditorCommands.DocumentStart)]
		protected void OnDocumentStart ()
		{
			doc.Editor.EditorActionHost.MoveCaretToDocumentStart ();
		}
		
		[CommandHandler (TextEditorCommands.DocumentEnd)]
		protected void OnDocumentEnd ()
		{
			doc.Editor.EditorActionHost.MoveCaretToDocumentEnd ();
		}
		
		[CommandHandler (TextEditorCommands.DeleteLine)]
		protected void OnDeleteLine ()
		{
			var line = doc.Editor.GetLine (doc.Editor.CaretLocation.Line);
			doc.Editor.RemoveText (line.Offset, line.LengthIncludingDelimiter);
		}
		
		struct RemoveInfo
		{
			public int Position { get; set; }
			public int Length { get; set; }

			public static readonly RemoveInfo Empty = new RemoveInfo (-1, -1);
			
			public bool IsEmpty {
				get {
					return Length <= 0;
				}
			}
			
			RemoveInfo (int position, int length): this ()
			{
				Position = position;
				Length = length;
			}

			public static bool IsWhiteSpace (char ch) 
			{
				return ch == ' ' || ch == '\t' || ch == '\v';
			}
			
			public static RemoveInfo GetRemoveInfo (TextEditor document, ref int pos)
			{
				int len = 0;
				while (pos > 0 && IsWhiteSpace (document.GetCharAt (pos))) {
					--pos;
					++len;
				}
				if (len > 0) {
					pos++;
					return new RemoveInfo (pos, len);
				}
				return Empty;
			}

			public override string ToString ()
			{
				return string.Format ("[RemoveInfo: Position={0}, Length={1}]", Position, Length);
			}
		}
		
		[CommandHandler (EditCommands.RemoveTrailingWhiteSpaces)]
		public void OnRemoveTrailingWhiteSpaces ()
		{
			var data = doc.Editor;
			if (data == null)
				return;
			
			System.Collections.Generic.List<RemoveInfo> removeList = new System.Collections.Generic.List<RemoveInfo> ();
			int pos = data.Length - 1;
			RemoveInfo removeInfo = RemoveInfo.GetRemoveInfo (data, ref pos);
			if (!removeInfo.IsEmpty)
				removeList.Add (removeInfo);
			
			while (pos >= 0) {
				char ch = data.GetCharAt (pos);
				if (ch == '\n' || ch == '\r') {
					if (RemoveInfo.IsWhiteSpace (data.GetCharAt (pos - 1))) {
						--pos;
						removeInfo = RemoveInfo.GetRemoveInfo (data, ref pos);
						if (!removeInfo.IsEmpty)
							removeList.Add (removeInfo);
					}
				}
				--pos;
			}
			using (var undo = data.OpenUndoGroup ()) {
				foreach (var info in removeList) {
					data.RemoveText (info.Position, info.Length);
				}
			}
		}
		
		[CommandUpdateHandler (EditCommands.RemoveTrailingWhiteSpaces)]
		protected void OnRemoveTrailingWhiteSpaces (CommandInfo info)
		{
			info.Enabled = GetContent <TextEditor> () != null;
		}
		
		#region Folding
		bool IsFoldMarkerMarginEnabled {
			get {
				return DefaultSourceEditorOptions.Instance.ShowFoldMargin;
			}
		}

		[CommandHandler (EditCommands.EnableDisableFolding)]
		protected void EnableDisableFolding ()
		{
			DefaultSourceEditorOptions.Instance.ShowFoldMargin = !DefaultSourceEditorOptions.Instance.ShowFoldMargin;
		}

		[CommandUpdateHandler (EditCommands.EnableDisableFolding)]
		protected void UpdateEnableDisableFolding (CommandInfo info)
		{
			info.Text = IsFoldMarkerMarginEnabled ? GettextCatalog.GetString ("Disable _Folding") : GettextCatalog.GetString ("Enable _Folding");
			info.Enabled = GetContent<IFoldable> () != null;
		}

		[CommandUpdateHandler (EditCommands.ToggleAllFoldings)]
		[CommandUpdateHandler (EditCommands.FoldDefinitions)]
		[CommandUpdateHandler (EditCommands.ToggleFolding)]
		protected void UpdateFoldCommands (CommandInfo info)
		{
			info.Enabled = GetContent <IFoldable> () != null && IsFoldMarkerMarginEnabled;
		}
		
		[CommandHandler (EditCommands.ToggleAllFoldings)]
		protected void ToggleAllFoldings ()
		{
			GetContent <IFoldable> ().ToggleAllFoldings ();
		}
		
		[CommandHandler (EditCommands.FoldDefinitions)]
		protected void FoldDefinitions ()
		{
			GetContent <IFoldable> ().FoldDefinitions ();
		}

		[CommandHandler (EditCommands.ToggleFolding)]
		protected void ToggleFolding ()
		{
			GetContent <IFoldable> ().ToggleFolding ();
		}
		
		#endregion
		
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
		
	}
}
