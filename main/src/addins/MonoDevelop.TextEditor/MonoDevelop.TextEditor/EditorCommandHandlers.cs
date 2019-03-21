//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.TextEditor.Cocoa
{
    [Name("Standalone Editor command handlers")]
    [ContentType("any")]
    [Order(After = DefaultOrderings.Lowest)]
    [Export(typeof(ICommandHandler))]
    class EditorCommandHandlers :
        ICommandHandler<BackspaceKeyCommandArgs>,
        ICommandHandler<BackTabKeyCommandArgs>,
        ICommandHandler<CutCommandArgs>,
        ICommandHandler<CopyCommandArgs>,
        ICommandHandler<DeleteKeyCommandArgs>,
        ICommandHandler<DownKeyCommandArgs>,
        ICommandHandler<EscapeKeyCommandArgs>,
        ICommandHandler<InsertSnippetCommandArgs>,
        ICommandHandler<LeftKeyCommandArgs>,
        ICommandHandler<LineEndCommandArgs>,
        ICommandHandler<LineEndExtendCommandArgs>,
        ICommandHandler<LineStartCommandArgs>,
        ICommandHandler<LineStartExtendCommandArgs>,
        ICommandHandler<MoveSelectedLinesDownCommandArgs>,
        ICommandHandler<MoveSelectedLinesUpCommandArgs>,
        ICommandHandler<PageDownKeyCommandArgs>,
        ICommandHandler<PageUpKeyCommandArgs>,
        ICommandHandler<PasteCommandArgs>,
        ICommandHandler<RedoCommandArgs>,
        ICommandHandler<RenameCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<RightKeyCommandArgs>,
        ICommandHandler<SaveCommandArgs>,
        ICommandHandler<SelectAllCommandArgs>,
        ICommandHandler<SurroundWithCommandArgs>,
        ICommandHandler<TabKeyCommandArgs>,
        ICommandHandler<ToggleCompletionModeCommandArgs>,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<UndoCommandArgs>,
        ICommandHandler<UpKeyCommandArgs>,
        ICommandHandler<WordDeleteToEndCommandArgs>,
        ICommandHandler<WordDeleteToStartCommandArgs>,
        ICommandHandler<FindReferencesCommandArgs>
#if !WINDOWS
        ,
		ICommandHandler<ProvideEditorFeedbackCommandArgs>,
		ICommandHandler<DisableEditorPreviewCommandArgs>,
		ICommandHandler<LearnAboutTheEditorCommandArgs>
#endif
	{
        [Import]
        private IEditorOperationsFactoryService OperationsService { get; set; }

        [Import]
        private ITextUndoHistoryRegistry _undoHistoryRegistry { get; set; }

        string INamed.DisplayName => "Standalone Editor command handlers";

        public IEditorOperations3 GetOperations (ITextView textView)
        {
            return (IEditorOperations3)OperationsService.GetEditorOperations(textView);
        }

        /// <summary>
        /// Returns<see cref="CommandState.Available"/> if <paramref name = "textView" /> has <see cref="PredefinedTextViewRoles.Editable"/>
        /// among <see cref="ITextView.Roles"/>, or <see cref="CommandState.Unavailable"/> otherwise.
        /// </summary>
        private static CommandState AvailableInEditableView(ITextView textView)
        {
            return textView.Roles.Contains(PredefinedTextViewRoles.Editable) ? CommandState.Available : CommandState.Unavailable;
        }

        bool ICommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).InsertNewLine();
            return true;
        }

        CommandState ICommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            if (args.TypedChar == '\0')
                return false;

            GetOperations(args.TextView).InsertText(args.TypedChar.ToString());
            return true;
        }

        CommandState ICommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args)
        {
            return args.TypedChar == '\0' ? CommandState.Unavailable : AvailableInEditableView(args.TextView);
        }

        CommandState ICommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).Backspace();
            return true;
        }

        CommandState ICommandHandler<BackTabKeyCommandArgs>.GetCommandState(BackTabKeyCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<BackTabKeyCommandArgs>.ExecuteCommand(BackTabKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).Unindent();
            return true;
        }

        CommandState ICommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).CutSelection();
            return true;
        }

        CommandState ICommandHandler<CopyCommandArgs>.GetCommandState(CopyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<CopyCommandArgs>.ExecuteCommand(CopyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).CopySelection();
            return true;
        }

        CommandState ICommandHandler<DeleteKeyCommandArgs>.GetCommandState(DeleteKeyCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<DeleteKeyCommandArgs>.ExecuteCommand(DeleteKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).Delete();
            return true;
        }

        CommandState ICommandHandler<DownKeyCommandArgs>.GetCommandState(DownKeyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<DownKeyCommandArgs>.ExecuteCommand(DownKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveLineDown(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<EscapeKeyCommandArgs>.GetCommandState(EscapeKeyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<EscapeKeyCommandArgs>.ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).ResetSelection();
            return true;
        }

        CommandState ICommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, CommandExecutionContext executionContext)
        {
            // Do nothing. We merely enable others to call this command
            return true;
        }

        CommandState ICommandHandler<LeftKeyCommandArgs>.GetCommandState(LeftKeyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<LeftKeyCommandArgs>.ExecuteCommand(LeftKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveToPreviousCharacter(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<LineEndCommandArgs>.GetCommandState(LineEndCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<LineEndCommandArgs>.ExecuteCommand(LineEndCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveToEndOfLine(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<LineEndExtendCommandArgs>.GetCommandState(LineEndExtendCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<LineEndExtendCommandArgs>.ExecuteCommand(LineEndExtendCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveToEndOfLine(extendSelection: true);
            return true;
        }

        CommandState ICommandHandler<LineStartCommandArgs>.GetCommandState(LineStartCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<LineStartCommandArgs>.ExecuteCommand(LineStartCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveToHome(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<LineStartExtendCommandArgs>.GetCommandState(LineStartExtendCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<LineStartExtendCommandArgs>.ExecuteCommand(LineStartExtendCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveToStartOfLine(extendSelection: true);
            return true;
        }

        CommandState ICommandHandler<MoveSelectedLinesDownCommandArgs>.GetCommandState(MoveSelectedLinesDownCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<MoveSelectedLinesDownCommandArgs>.ExecuteCommand(MoveSelectedLinesDownCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations (args.TextView).MoveSelectedLinesDown ();
            return true;
        }

        CommandState ICommandHandler<MoveSelectedLinesUpCommandArgs>.GetCommandState(MoveSelectedLinesUpCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<MoveSelectedLinesUpCommandArgs>.ExecuteCommand(MoveSelectedLinesUpCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations (args.TextView).MoveSelectedLinesUp ();
            return true;
        }

        CommandState ICommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).PageDown(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).PageUp(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).Paste();
            return true;
        }

        CommandState ICommandHandler<RedoCommandArgs>.GetCommandState(RedoCommandArgs args)
        {
            return GetUndoHistory(args.TextView).CanRedo ? CommandState.Available : CommandState.Unavailable;
        }

        bool ICommandHandler<RedoCommandArgs>.ExecuteCommand(RedoCommandArgs args, CommandExecutionContext executionContext)
        {
            GetUndoHistory(args.TextView).Redo(args.Count);
            return true;
        }

        CommandState ICommandHandler<RenameCommandArgs>.GetCommandState(RenameCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<RenameCommandArgs>.ExecuteCommand(RenameCommandArgs args, CommandExecutionContext executionContext)
        {
            // Do nothing. We merely enable others to call this command
            return true;
        }

        CommandState ICommandHandler<FindReferencesCommandArgs>.GetCommandState (FindReferencesCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<FindReferencesCommandArgs>.ExecuteCommand (FindReferencesCommandArgs args, CommandExecutionContext executionContext)
        {
            // Do nothing. We merely enable others to call this command
            return true;
        }

        CommandState ICommandHandler<RightKeyCommandArgs>.GetCommandState(RightKeyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<RightKeyCommandArgs>.ExecuteCommand(RightKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveToNextCharacter(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<SaveCommandArgs>.GetCommandState(SaveCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<SaveCommandArgs>.ExecuteCommand(SaveCommandArgs args, CommandExecutionContext executionContext)
        {
            // Do nothing. We merely enable others to call this command
            return true;
        }

        CommandState ICommandHandler<SelectAllCommandArgs>.GetCommandState(SelectAllCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<SelectAllCommandArgs>.ExecuteCommand(SelectAllCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).SelectAll();
            return true;
        }

        CommandState ICommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, CommandExecutionContext executionContext)
        {
            // Do nothing. We merely enable others to call this command
            return true;
        }

        CommandState ICommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).Indent();
            return true;
        }

        CommandState ICommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args, CommandExecutionContext executionContext)
        {
            // Do nothing. We merely enable others to call this command
            return true;
        }

        private ITextUndoHistory GetUndoHistory(ITextView textView)
        {
            return _undoHistoryRegistry.GetHistory(textView.TextBuffer);
        }

        CommandState ICommandHandler<UndoCommandArgs>.GetCommandState(UndoCommandArgs args)
        {
            return GetUndoHistory(args.TextView).CanUndo ? CommandState.Available : CommandState.Unavailable;
        }

        bool ICommandHandler<UndoCommandArgs>.ExecuteCommand(UndoCommandArgs args, CommandExecutionContext executionContext)
        {
            GetUndoHistory(args.TextView).Undo(args.Count);
            return true;
        }

        CommandState ICommandHandler<UpKeyCommandArgs>.GetCommandState(UpKeyCommandArgs args)
        {
            return CommandState.Available;
        }

        bool ICommandHandler<UpKeyCommandArgs>.ExecuteCommand(UpKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).MoveLineUp(extendSelection: false);
            return true;
        }

        CommandState ICommandHandler<WordDeleteToEndCommandArgs>.GetCommandState(WordDeleteToEndCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<WordDeleteToEndCommandArgs>.ExecuteCommand(WordDeleteToEndCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).DeleteToEndOfLine();
            return true;
        }

        CommandState ICommandHandler<WordDeleteToStartCommandArgs>.GetCommandState(WordDeleteToStartCommandArgs args)
        {
            return AvailableInEditableView(args.TextView);
        }

        bool ICommandHandler<WordDeleteToStartCommandArgs>.ExecuteCommand(WordDeleteToStartCommandArgs args, CommandExecutionContext executionContext)
        {
            GetOperations(args.TextView).DeleteToBeginningOfLine();
            return true;
        }

		#region Preview Editor Commands

#if !WINDOWS

		CommandState ICommandHandler<ProvideEditorFeedbackCommandArgs>.GetCommandState (ProvideEditorFeedbackCommandArgs args)
			=> CommandState.Available;

		bool ICommandHandler<ProvideEditorFeedbackCommandArgs>.ExecuteCommand (ProvideEditorFeedbackCommandArgs args, CommandExecutionContext executionContext)
		{
			IdeServices.DesktopService.ShowUrl ("https://aka.ms/vs/mac/editor/report-problem");
			return true;
		}

		CommandState ICommandHandler<LearnAboutTheEditorCommandArgs>.GetCommandState (LearnAboutTheEditorCommandArgs args)
			=> CommandState.Available;

		bool ICommandHandler<LearnAboutTheEditorCommandArgs>.ExecuteCommand (LearnAboutTheEditorCommandArgs args, CommandExecutionContext executionContext)
		{
			IdeServices.DesktopService.ShowUrl ("https://aka.ms/vs/mac/editor/learn-more");
			return true;
		}

		CommandState ICommandHandler<DisableEditorPreviewCommandArgs>.GetCommandState (DisableEditorPreviewCommandArgs args)
			=> CommandState.Available;

		bool ICommandHandler<DisableEditorPreviewCommandArgs>.ExecuteCommand (DisableEditorPreviewCommandArgs args, CommandExecutionContext executionContext)
		{
			DefaultSourceEditorOptions.Instance.EnableNewEditor = false;
			return true;
		}

#endif

#endregion
	}
}
