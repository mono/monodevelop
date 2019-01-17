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
	
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Refactoring;

namespace MonoDevelop.TextEditor
{
	partial class TextViewContent
	{
		void UpdateCommand (CommandState commandState, CommandInfo commandInfo)
		{
			commandInfo.Enabled = commandState.IsAvailable;
			commandInfo.Visible = !commandState.IsUnspecified;
			commandInfo.Checked = commandState.IsChecked;
		}

		CommandState UpdateCommand<T> (CommandInfo commandInfo, Func<ITextView, ITextBuffer, T> factory) where T : EditorCommandArgs
		{
			Console.WriteLine ("Update command: {0} -> {1}", typeof (T), commandInfo.Command.Id);
			var commandState = commandService.GetCommandState (factory, null);
			UpdateCommand (commandState, commandInfo);
			return commandState;
		}

		void ExecCommand<T> (Func<ITextView, ITextBuffer, T> factory) where T : EditorCommandArgs
		{
			Console.WriteLine ("Exec command: {0}", typeof (T));
			commandService.Execute (factory, null);
		}

		#region Code Formatting Commands

		// Missing:
		//   CodeFormattingCommands.FormatBuffer

		#endregion

		#region Edit Commands

		// Missing:
		//   Delete,
		//   ToggleCodeComment,
		//   IndentSelection,
		//   UnIndentSelection,
		//   UppercaseSelection,
		//   LowercaseSelection,
		//   RemoveTrailingWhiteSpaces,
		//   JoinWithNextLine,
		//   MonodevelopPreferences,
		//   DefaultPolicies,
		//   InsertStandardHeader,
		//   EnableDisableFolding,
		//   ToggleFolding,
		//   ToggleAllFoldings,
		//   FoldDefinitions,
		//   InsertGuid,
		//   SortSelectedLines

		[CommandUpdateHandler (EditCommands.Copy)]
		void UpdateCopyCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new CopyCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.Copy)]
		void ExecCopyCommand ()
			 => ExecCommand ((textView, textBuffer) => new CopyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.Cut)]
		void UpdateCutCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new CutCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.Cut)]
		void ExecCutCommand ()
			 => ExecCommand ((textView, textBuffer) => new CutCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.Paste)]
		void UpdatePasteCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new PasteCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.Paste)]
		void ExecPasteCommand ()
			 => ExecCommand ((textView, textBuffer) => new PasteCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.Rename)]
		void UpdateRenameCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new RenameCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.Rename)]
		void ExecRenameCommand ()
			 => ExecCommand ((textView, textBuffer) => new RenameCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.Undo)]
		void UpdateUndoCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new UndoCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.Undo)]
		void ExecUndoCommand ()
			 => ExecCommand ((textView, textBuffer) => new UndoCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.Redo)]
		void UpdateRedoCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new RedoCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.Redo)]
		void ExecRedoCommand ()
			 => ExecCommand ((textView, textBuffer) => new RedoCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.SelectAll)]
		void UpdateSelectAllCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new SelectAllCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.SelectAll)]
		void ExecSelectAllCommand ()
			 => ExecCommand ((textView, textBuffer) => new SelectAllCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.AddCodeComment)]
		void UpdateAddCodeCommentCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new CommentSelectionCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.AddCodeComment)]
		void ExecAddCodeCommentCommand ()
			 => ExecCommand ((textView, textBuffer) => new CommentSelectionCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.RemoveCodeComment)]
		void UpdateRemoveCodeCommentCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new UncommentSelectionCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.RemoveCodeComment)]
		void ExecRemoveCodeCommentCommand ()
			 => ExecCommand ((textView, textBuffer) => new UncommentSelectionCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.DeleteKey)]
		void UpdateDeleteKeyCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new BackspaceKeyCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.DeleteKey)]
		void ExecDeleteKeyCommand ()
			 => ExecCommand ((textView, textBuffer) => new BackspaceKeyCommandArgs (textView, textBuffer));

		#endregion

		#region Refactory Commands

		// Missing:
		//   CurrentRefactoryOperations
		//   FindReferences
		//   FindAllReferences
		//   FindDerivedClasses
		//   DeclareLocal
		//   ImportSymbol
		//   QuickFix
		//   QuickFixMenu

		[CommandUpdateHandler (RefactoryCommands.GotoDeclaration)]
		void UpdateGotoDeclarationCommand (CommandInfo info)
 			 => UpdateCommand (info, (textView, textBuffer) => new GoToDefinitionCommandArgs (textView, textBuffer));

		[CommandHandler (RefactoryCommands.GotoDeclaration)]
		void ExecGotoDeclarationCommand ()
			 => ExecCommand ((textView, textBuffer) => new GoToDefinitionCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (RefactoryCommands.FindReferences)]
		void UpdateFindReferencesCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new FindReferencesCommandArgs (textView, textBuffer));

		[CommandHandler (RefactoryCommands.FindReferences)]
		void ExecFindReferencesCommand ()
			 => ExecCommand ((textView, textBuffer) => new FindReferencesCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (RefactoryCommands.FindAllReferences)]
		void UpdateFindAllReferencesCommand (CommandInfo info)
			 => UpdateCommand (info, (textView, textBuffer) => new FindReferencesCommandArgs (textView, textBuffer));

		[CommandHandler (RefactoryCommands.FindAllReferences)]
		void ExecFindAllReferencesCommand ()
			 => ExecCommand ((textView, textBuffer) => new FindReferencesCommandArgs (textView, textBuffer));

		#endregion

		#region Editor Commands

		// Missing:
		//   ShowCodeTemplateWindow
		//   ShowCodeSurroundingsWindow
		//   DeleteLeftChar
		//   DeleteRightChar
		//   ScrollLineUp
		//   ScrollLineDown
		//   ScrollPageUp
		//   ScrollPageDown
		//   ScrollTop
		//   ScrollBottom
		//   DeleteLine
		//   DeleteToLineStart
		//   DeleteToLineEnd
		//   MoveBlockUp
		//   MoveBlockDown
		//   ShowParameterCompletionWindow
		//   GotoMatchingBrace
		//   SelectionMoveLeft
		//   SelectionMoveRight
		//   MovePrevWord
		//   MoveNextWord
		//   SelectionMovePrevWord
		//   SelectionMoveNextWord
		//   SelectionMoveUp
		//   SelectionMoveDown
		//   SelectionMoveHome
		//   SelectionMoveEnd
		//   SelectionMoveToDocumentStart
		//   SelectionMoveToDocumentEnd
		//   ExpandSelectionToLine
		//   ExpandSelection
		//   ShrinkSelection
		//   SwitchCaretMode
		//   InsertTab
		//   RemoveTab
		//   InsertNewLine
		//   InsertNewLinePreserveCaretPosition
		//   InsertNewLineAtEnd
		//   CompleteStatement
		//   DeletePrevWord
		//   DeleteNextWord
		//   SelectionPageDownAction
		//   SelectionPageUpAction
		//   MovePrevSubword
		//   MoveNextSubword
		//   SelectionMovePrevSubword
		//   SelectionMoveNextSubword
		//   DeletePrevSubword
		//   DeleteNextSubword
		//   TransposeCharacters
		//   RecenterEditor
		//   DuplicateLine
		//   ToggleCompletionSuggestionMode
		//   ToggleBlockSelectionMode
		//   DynamicAbbrev
		//   PulseCaret
		//   ShowQuickInfo

		[CommandUpdateHandler (TextEditorCommands.ShowCompletionWindow)]
		void UpdateShowCompletionWindowCommand (CommandInfo info)
			=> UpdateCommand (info, (textView, textBuffer) => new InvokeCompletionListCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.ShowCompletionWindow)]
		void ExecShowCompletionWindowCommand ()
			=> ExecCommand ((textView, textBuffer) => new InvokeCompletionListCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.LineEnd)]
		void UpdateLineEndCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new LineEndCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.LineEnd)]
		void ExecLineEndCommand ()
			=> ExecCommand ((textView, textBuffer) => new LineEndCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.LineStart)]
		void UpdateLineStartCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new LineStartCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.LineStart)]
		void ExecLineStartCommand ()
			=> ExecCommand ((textView, textBuffer) => new LineStartCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.CharLeft)]
		void UpdateCharLeftCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new LeftKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.CharLeft)]
		void ExecCharLeftCommand ()
			=> ExecCommand ((textView, textBuffer) => new LeftKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.CharRight)]
		void UpdateCharRightCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new RightKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.CharRight)]
		void ExecCharRightCommand ()
			=> ExecCommand ((textView, textBuffer) => new RightKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.LineUp)]
		void UpdateLineUpCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new UpKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.LineUp)]
		void ExecLineUpCommand ()
			=> ExecCommand ((textView, textBuffer) => new UpKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.LineDown)]
		void UpdateLineDownCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new DownKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.LineDown)]
		void ExecLineDownCommand ()
			=> ExecCommand ((textView, textBuffer) => new DownKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.PageUp)]
		void UpdatePageUpCommand (CommandInfo info)
	 		=> UpdateCommand (info, (textView, textBuffer) => new PageUpKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.PageUp)]
		void ExecPageUpCommand ()
			=> ExecCommand ((textView, textBuffer) => new PageUpKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.PageDown)]
		void UpdatePageDownCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new PageDownKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.PageDown)]
		void ExecPageDownCommand ()
			=> ExecCommand ((textView, textBuffer) => new PageDownKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.DocumentStart)]
		void UpdateDocumentStartCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new DocumentStartCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.DocumentStart)]
		void ExecDocumentStartCommand ()
			=> ExecCommand ((textView, textBuffer) => new DocumentStartCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.DocumentEnd)]
		void UpdateDocumentEndCommand (CommandInfo info)
 			=> UpdateCommand (info, (textView, textBuffer) => new DocumentEndCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.DocumentEnd)]
		void ExecDocumentEndCommand ()
			=> ExecCommand ((textView, textBuffer) => new DocumentEndCommandArgs (textView, textBuffer));

		#endregion
	}
}