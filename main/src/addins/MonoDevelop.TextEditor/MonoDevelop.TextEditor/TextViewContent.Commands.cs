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
using MonoDevelop.Components.Commands;

namespace MonoDevelop.TextEditor
{
	partial class TextViewContent<TView, TImports>
	{
		ICommandHandler ICustomCommandTarget.GetCommandHandler (object commandId)
		{
			if (CommandMappings.Instance.HasMapping (commandId)) {
				return this;
			}
			return null;
		}

		ICommandUpdater ICustomCommandTarget.GetCommandUpdater (object commandId)
		{
			if (CommandMappings.Instance.HasMapping (commandId)) {
				return this;
			}
			return null;
		}

		void ICommandHandler.Run (object cmdTarget, Command cmd)
		{
			var mapping = CommandMappings.Instance.GetMapping (cmd.Id);
			if (mapping != null) {
				mapping.Execute (commandService, null);
			}
		}

		void ICommandHandler.Run (object cmdTarget, Command cmd, object dataItem)
		{
			throw new InvalidOperationException ("Array commands cannot be mapped to editor commands");
		}

		void ICommandUpdater.Run (object cmdTarget, CommandInfo info)
		{
			var mapping = CommandMappings.Instance.GetMapping (info.Command.Id);
			if (mapping != null) {
				var commandState = mapping.GetCommandState (commandService, null);
				info.Enabled = commandState.IsAvailable;
				info.Visible = !commandState.IsUnspecified;
				info.Checked = commandState.IsChecked;
			}
		}

		void ICommandUpdater.Run (object cmdTarget, CommandArrayInfo info)
		{
			throw new InvalidOperationException ("Array commands cannot be mapped to editor commands");
		}

		// Missing EditCommands:
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

		[CommandHandler (EditCommands.InsertGuid)]
		void ExecInsertGuidCommand ()
			=> editorOperations.InsertText (Guid.NewGuid ().ToString ());

		// Missing RefactoryCommands:
		//   CurrentRefactoryOperations
		//   FindReferences
		//   FindAllReferences
		//   FindDerivedClasses
		//   DeclareLocal
		//   ImportSymbol
		//   QuickFix
		//   QuickFixMenu

		// Missing TextEditorCommands
		//   ShowCodeTemplateWindow
		//   ShowCodeSurroundingsWindow
		//   MoveBlockUp
		//   MoveBlockDown
		//   ShowParameterCompletionWindow
		//   GotoMatchingBrace
		//   ShrinkSelection
		//   CompleteStatement
		//   MovePrevSubword
		//   MoveNextSubword
		//   SelectionMovePrevSubword
		//   SelectionMoveNextSubword
		//   DeletePrevSubword
		//   DeleteNextSubword
		//   ToggleCompletionSuggestionMode
		//   ToggleBlockSelectionMode
		//   DynamicAbbrev
		//   PulseCaret
		//   ShowQuickInfo

		[CommandHandler (TextEditorCommands.ScrollLineUp)]
		void ExecScrollLineUpCommand ()
			=> editorOperations.ScrollUpAndMoveCaretIfNecessary ();

		[CommandHandler (TextEditorCommands.ScrollLineDown)]
		void ExecScrollLineDownCommand ()
			=> editorOperations.ScrollDownAndMoveCaretIfNecessary ();

		[CommandHandler (TextEditorCommands.ScrollPageUp)]
		void ExecScrollPageUpCommand ()
			=> editorOperations.ScrollPageUp ();

		[CommandHandler (TextEditorCommands.ScrollPageDown)]
		void ExecScrollPageDownCommand ()
			=> editorOperations.ScrollPageDown ();

		[CommandHandler (TextEditorCommands.ScrollTop)]
		void ExecScrollTopCommand ()
			=> editorOperations.ScrollLineTop ();

		[CommandHandler (TextEditorCommands.ScrollBottom)]
		void ExecScrollBottomCommand ()
			=> editorOperations.ScrollLineBottom ();

		[CommandHandler (TextEditorCommands.InsertNewLine)]
		void ExecInsertNewLineCommand ()
			=> editorOperations.InsertNewLine ();

		[CommandHandler (TextEditorCommands.InsertNewLineAtEnd)]
		void ExecInsertNewLineAtEndCommand ()
			=> editorOperations.InsertFinalNewLine ();

		[CommandHandler (TextEditorCommands.InsertNewLinePreserveCaretPosition)]
		void ExecInsertNewLinePreserveCaretPositionCommand ()
			=> editorOperations.OpenLineAbove ();

		[CommandHandler (TextEditorCommands.TransposeCharacters)]
		void ExecTransposeCharactersCommand ()
			=> editorOperations.TransposeCharacter ();

		[CommandHandler (TextEditorCommands.DeleteLine)]
		void ExecDeleteLineCommand ()
			=> editorOperations.DeleteFullLine ();

		[CommandHandler (TextEditorCommands.DeleteToLineStart)]
		void ExecDeleteToLineStartCommand ()
			=> editorOperations.DeleteToBeginningOfLine ();

		[CommandHandler (TextEditorCommands.DeleteToLineEnd)]
		void ExecDeleteToLineEndCommand ()
			=> editorOperations.DeleteToEndOfLine ();

		[CommandHandler (TextEditorCommands.DeletePrevWord)]
		void ExecDeletePrevWordCommand ()
			=> editorOperations.DeleteWordToLeft ();

		[CommandHandler (TextEditorCommands.DeleteNextWord)]
		void ExecDeleteNextWordCommand ()
			=> editorOperations.DeleteWordToRight ();

		[CommandHandler (TextEditorCommands.ExpandSelection)]
		void ExecExpandSelectionCommand ()
			=> editorOperations.SelectEnclosing ();

		[CommandHandler (TextEditorCommands.ExpandSelectionToLine)]
		void ExecExpandSelectionToLineCommand ()
			=> editorOperations.MoveToEndOfLine (extendSelection: true);

		[CommandHandler (TextEditorCommands.MovePrevWord)]
		void ExecMovePrevWordCommand ()
			=> editorOperations.MoveToPreviousWord (extendSelection: false);

		[CommandHandler (TextEditorCommands.MoveNextWord)]
		void ExecMoveNextWordCommand ()
			=> editorOperations.MoveToNextWord (extendSelection: false);

		[CommandHandler (TextEditorCommands.SelectionMoveLeft)]
		void ExecSelectionMoveLeftCommand ()
			=> editorOperations.MoveToPreviousCharacter (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveRight)]
		void ExecSelectionMoveRightCommand ()
			=> editorOperations.MoveToNextCharacter (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMovePrevWord)]
		void ExecSelectionMovePrevWordCommand ()
			=> editorOperations.MoveToPreviousWord (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveNextWord)]
		void ExecSelectionMoveNextWordCommand ()
			=> editorOperations.MoveToNextWord (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveUp)]
		void ExecSelectionMoveUpCommand ()
			=> editorOperations.MoveLineUp (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveDown)]
		void ExecSelectionMoveDownCommand ()
			=> editorOperations.MoveLineDown (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveHome)]
		void ExecSelectionMoveHomeCommand ()
			=> editorOperations.MoveToHome (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveEnd)]
		void ExecSelectionMoveEndCommand ()
			=> editorOperations.MoveToEndOfLine (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveToDocumentStart)]
		void ExecSelectionMoveToDocumentStartCommand ()
			=> editorOperations.MoveToStartOfDocument (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionMoveToDocumentEnd)]
		void ExecSelectionMoveToDocumentEndCommand ()
			=> editorOperations.MoveToEndOfDocument (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionPageUpAction)]
		void ExecSelectionPageUpActionStartCommand ()
			=> editorOperations.PageUp (extendSelection: true);

		[CommandHandler (TextEditorCommands.SelectionPageDownAction)]
		void ExecSelectionPageDownActiondCommand ()
			=> editorOperations.PageDown (extendSelection: true);

		[CommandHandler (TextEditorCommands.RecenterEditor)]
		[CommandHandler (ViewCommands.CenterAndFocusCurrentDocument)]
		void ExecCenterAndFocusCurrentDocumentCommand ()
			=> editorOperations.ScrollLineCenter ();

		[CommandHandler (TextEditorCommands.DuplicateLine)]
		void ExecDuplicateLineCommand ()
			=> editorOperations.DuplicateSelection ();

		[CommandHandler (TextEditorCommands.SwitchCaretMode)]
		void ExecSwitchCaretMode ()
		{
			var overWriteMode = editorOptions.GetOptionValue (DefaultTextViewOptions.OverwriteModeId);
			editorOptions.SetOptionValue (DefaultTextViewOptions.OverwriteModeId, !overWriteMode);
		}

		[CommandUpdateHandler (TextEditorCommands.InsertTab)]
		void UpdateInsertTabCommand (CommandInfo info)
			=> UpdateCommand (info, (textView, textBuffer) => new TabKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.InsertTab)]
		void ExecInsertTabCommand ()
			=> ExecCommand ((textView, textBuffer) => new TabKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.RemoveTab)]
		void UpdateRemoveTabCommand (CommandInfo info)
			=> UpdateCommand (info, (textView, textBuffer) => new BackTabKeyCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.RemoveTab)]
		void ExecRemoveTabCommand ()
			=> ExecCommand ((textView, textBuffer) => new BackTabKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (TextEditorCommands.ShowCompletionWindow)]
		void UpdateShowCompletionWindowCommand (CommandInfo info)
			=> UpdateCommand (info, (textView, textBuffer) => new InvokeCompletionListCommandArgs (textView, textBuffer));

		[CommandHandler (TextEditorCommands.ShowCompletionWindow)]
		void ExecShowCompletionWindowCommand ()
			=> ExecCommand ((textView, textBuffer) => new InvokeCompletionListCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (TextEditorCommands.DeleteRightChar)]
		void UpdateDeleteRightCharCommand (CommandInfo info)
			=> UpdateCommand (info, (textView, textBuffer) => new DeleteKeyCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.Delete)]
		[CommandHandler (TextEditorCommands.DeleteRightChar)]
		void ExecDeleteRightCharCommand ()
			=> ExecCommand ((textView, textBuffer) => new DeleteKeyCommandArgs (textView, textBuffer));

		[CommandUpdateHandler (EditCommands.DeleteKey)]
		[CommandUpdateHandler (TextEditorCommands.DeleteLeftChar)]
		void UpdateDeleteLeftCharCommand (CommandInfo info)
			=> UpdateCommand (info, (textView, textBuffer) => new BackspaceKeyCommandArgs (textView, textBuffer));

		[CommandHandler (EditCommands.DeleteKey)]
		[CommandHandler (TextEditorCommands.DeleteLeftChar)]
		void ExecDeleteLeftCharCommand ()
			=> ExecCommand ((textView, textBuffer) => new BackspaceKeyCommandArgs (textView, textBuffer));

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
	}
}