using System;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Refactoring;

namespace MonoDevelop.Ide.Text
{
	partial class TextViewContent
	{
		[CommandUpdateHandler (EditCommands.AddCodeComment)]
		void AddCodeComment (CommandInfo info)
		{
			var commandState = _editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new CommentSelectionCommandArgs (textView, textBuffer), null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (EditCommands.AddCodeComment)]
		void AddCodeComment ()
		{
			_editorCommandHandlerService.Execute ((textView, textBuffer) => new CommentSelectionCommandArgs (textView, textBuffer), null);
		}

		[CommandUpdateHandler (EditCommands.RemoveCodeComment)]
		void RemoveCodeComment (CommandInfo info)
		{
			var commandState = _editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new UncommentSelectionCommandArgs (textView, textBuffer), null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (EditCommands.RemoveCodeComment)]
		void RemoveCodeComment ()
		{
			_editorCommandHandlerService.Execute ((textView, textBuffer) => new UncommentSelectionCommandArgs (textView, textBuffer), null);
		}

		[CommandUpdateHandler (RefactoryCommands.GotoDeclaration)]
		void GotoDeclaration (CommandInfo info)
		{
			var commandState = _editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new GoToDefinitionCommandArgs (textView, textBuffer), null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (RefactoryCommands.GotoDeclaration)]
		void GotoDeclaration ()
		{
			_editorCommandHandlerService.Execute ((textView, textBuffer) => new GoToDefinitionCommandArgs (textView, textBuffer), null);
		}
	}
}
