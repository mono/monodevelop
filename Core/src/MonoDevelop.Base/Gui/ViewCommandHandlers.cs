
using System;
using System.Collections;
using System.IO;
using Gtk;

using Gdl;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using MonoDevelop.Gui.Utils;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui
{
	public class ViewCommandHandlers: ICommandRouter
	{
		IWorkbenchWindow window;
		object nextTarget;

		public ViewCommandHandlers (IWorkbenchWindow window)
		{
			this.window = window;
		}
		
		object ICommandRouter.GetNextCommandTarget ()
		{
			return nextTarget;
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
			Runtime.FileService.SaveFile (window);
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
				info.Enabled = content.IsDirty;
			else
				info.Enabled = false;
		}

		[CommandHandler (FileCommands.SaveAs)]
		protected void OnSaveFileAs ()
		{
			Runtime.FileService.SaveFileAs (window);
		}
		
		[CommandHandler (FileCommands.ReloadFile)]
		protected void OnReloadFile ()
		{
			if (Runtime.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure that you want to reload the file?"))) {
				IXmlConvertable memento = null;
				if (window.ViewContent is IMementoCapable) {
					memento = ((IMementoCapable)window.ViewContent).CreateMemento();
				}
				window.ViewContent.Load(window.ViewContent.ContentName);
				if (memento != null) {
					((IMementoCapable)window.ViewContent).SetMemento(memento);
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
			IEditable editable = window.ActiveViewContent as IEditable;
			if (editable != null)
				editable.Undo();
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo info)
		{
			info.Enabled = window.ActiveViewContent is IEditable;
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			if (editable != null) {
				editable.Redo();
			}
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo info)
		{
			info.Enabled = window.ActiveViewContent is IEditable;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			if (editable != null)
				editable.ClipboardHandler.Cut(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo info)
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			info.Enabled = editable != null && editable.ClipboardHandler.EnableCut;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			if (editable != null)
				editable.ClipboardHandler.Copy(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo info)
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			info.Enabled = editable != null && editable.ClipboardHandler.EnableCopy;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			if (editable != null)
				editable.ClipboardHandler.Paste(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo info)
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			info.Enabled = editable != null && editable.ClipboardHandler.EnablePaste;
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			if (editable != null)
				editable.ClipboardHandler.Delete(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo info)
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			info.Enabled = editable != null && editable.ClipboardHandler.EnableDelete;
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAll ()
		{
			IEditable editable = window.ActiveViewContent as IEditable;
			if (editable != null)
				editable.ClipboardHandler.SelectAll(null, null);
		}
		
		[CommandUpdateHandler (EditCommands.SelectAll)]
		protected void OnUpdateSelectAll (CommandInfo info)
		{
			IEditable editable = window.ActiveViewContent as IEditable;
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
			ICodeStyleOperations  styling = window.ActiveViewContent as ICodeStyleOperations;
			if (styling != null)
				styling.CommentCode ();
		}
		
		[CommandUpdateHandler (EditCommands.CommentCode)]
		protected void OnUpdateCommentCode (CommandInfo info)
		{
			info.Enabled = window.ActiveViewContent is ICodeStyleOperations;
		}
		
		[CommandHandler (EditCommands.UncommentCode)]
		public void OnUncommentCode()
		{
			ICodeStyleOperations  styling = window.ActiveViewContent as ICodeStyleOperations;
			if (styling != null)
				styling.UncommentCode ();
		}
		
		[CommandUpdateHandler (EditCommands.UncommentCode)]
		protected void OnUpdateUncommentCode (CommandInfo info)
		{
			info.Enabled = window.ActiveViewContent is ICodeStyleOperations;
		}
		
		[CommandHandler (EditCommands.IndentSelection)]
		public void OnIndentSelection()
		{
			ICodeStyleOperations  styling = window.ActiveViewContent as ICodeStyleOperations;
			if (styling != null)
				styling.IndentSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.IndentSelection)]
		protected void OnUpdateIndentSelection (CommandInfo info)
		{
			info.Enabled = window.ActiveViewContent is ICodeStyleOperations;
		}
		
		[CommandHandler (EditCommands.UnIndentSelection)]
		public void OnUnIndentSelection()
		{
			ICodeStyleOperations  styling = window.ActiveViewContent as ICodeStyleOperations;
			if (styling != null)
				styling.UnIndentSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.UnIndentSelection)]
		protected void OnUpdateUnIndentSelection (CommandInfo info)
		{
			info.Enabled = window.ActiveViewContent is ICodeStyleOperations;
		}
	}
}
