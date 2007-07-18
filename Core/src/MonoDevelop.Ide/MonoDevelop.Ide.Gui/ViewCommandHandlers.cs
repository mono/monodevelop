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
using System.IO;
using Gtk;

using Gdl;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Utils;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui
{
	public class ViewCommandHandlers: ICommandDelegatorRouter
	{
		IWorkbenchWindow window;
		object nextTarget;
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
		
		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			return nextTarget;
		}
		
		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
			return doc.ExtendedCommandTargetChain;
		}
		
		public void SetNextCommandTarget (object nextTarget)
		{
			this.nextTarget = nextTarget;
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
			if (Services.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure that you want to reload the file?"))) {
				IXmlConvertable memento = null;
				IMementoCapable mc = GetContent<IMementoCapable> ();
				if (mc != null) {
					memento = mc.CreateMemento();
				}
				window.ViewContent.Load (window.ViewContent.ContentName);
				if (memento != null) {
					mc.SetMemento(memento);
				}
			}
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
			info.Bypass = GetContent <IEditableTextBuffer> () == null;
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
			info.Bypass = GetContent <IEditableTextBuffer> () == null;
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
		
/*		[CommandHandler (EditCommands.Delete)]
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
*/		
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
			info.Enabled = editable != null && editable.ClipboardHandler.EnableSelectAll;
		}
		
		[CommandHandler (EditCommands.WordCount)]
		protected void OnWordCount()
		{
			WordCountDialog wcd = new WordCountDialog ();
			wcd.Run ();
			wcd.Hide ();
		}
		
		[CommandHandler (EditCommands.CommentCode)]
		public void OnCommentCode()
		{
			ICodeStyleOperations  styling = GetContent<ICodeStyleOperations> ();
			if (styling != null)
				styling.CommentCode ();
		}
		
		[CommandUpdateHandler (EditCommands.CommentCode)]
		protected void OnUpdateCommentCode (CommandInfo info)
		{
			info.Enabled = GetContent<ICodeStyleOperations> () != null;
		}
		
		[CommandHandler (EditCommands.UncommentCode)]
		public void OnUncommentCode()
		{
			ICodeStyleOperations  styling = GetContent <ICodeStyleOperations> ();
			if (styling != null)
				styling.UncommentCode ();
		}
		
		[CommandUpdateHandler (EditCommands.UncommentCode)]
		protected void OnUpdateUncommentCode (CommandInfo info)
		{
			info.Enabled = GetContent<ICodeStyleOperations> () != null;
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
		
		[CommandUpdateHandler (EditCommands.UnIndentSelection)]
		protected void OnUppercaseSelection (CommandInfo info)
		{
			info.Enabled = GetContent <IEditableTextBuffer> () != null;
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
		
		[CommandHandler (TextEditorCommands.DeleteLine)]
		protected void OnDeleteLine ()
		{
			doc.TextEditor.DeleteLine (doc.TextEditor.CursorLine);
			doc.TextEditor.ShowCursorPosition ();
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
	}
}
