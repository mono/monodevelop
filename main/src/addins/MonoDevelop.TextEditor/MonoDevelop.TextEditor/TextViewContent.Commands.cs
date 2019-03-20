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

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

#if WINDOWS
using EditorOperationsInterface = Microsoft.VisualStudio.Text.Operations.IEditorOperations3;
#else
using EditorOperationsInterface = Microsoft.VisualStudio.Text.Operations.IEditorOperations4;
#endif

namespace MonoDevelop.TextEditor
{
	partial class TextViewContent<TView, TImports>
	{
		#region IEditorOperations Command Mapping

		protected readonly struct EditorOperationCommand
		{
			public readonly Action<EditorOperationsInterface> Execute;
			public readonly Action<EditorOperationsInterface, CommandInfo> Update;

			public EditorOperationCommand (
				Action<EditorOperationsInterface> execute,
				Action<EditorOperationsInterface, CommandInfo> update = null)
			{
				Execute = execute;
				Update = update;
			}
		}

		protected sealed class EditorOperationsMap : Dictionary<object, EditorOperationCommand>
		{
			public new Action<EditorOperationsInterface> this[object id] {
				get => base[id].Execute;
				set => base[CommandManager.ToCommandId (id)] = new EditorOperationCommand (value);
			}

			public new void Add (object id, EditorOperationCommand command)
				=> base[CommandManager.ToCommandId (id)] = command;
		}

		protected readonly EditorOperationsMap EditorOperationCommands = new EditorOperationsMap {
			[TextEditorCommands.ScrollLineUp] = op => op.ScrollUpAndMoveCaretIfNecessary (),
			[TextEditorCommands.ScrollLineDown] = op => op.ScrollDownAndMoveCaretIfNecessary (),
			[TextEditorCommands.ScrollPageUp] = op => op.ScrollPageUp (),
			[TextEditorCommands.ScrollPageDown] = op => op.ScrollPageDown (),
			[TextEditorCommands.ScrollTop] = op => op.ScrollLineTop (),
			[TextEditorCommands.ScrollBottom] = op => op.ScrollLineBottom (),

			[TextEditorCommands.InsertNewLine] = op => op.InsertNewLine (),
			[TextEditorCommands.InsertNewLineAtEnd] = op => op.InsertFinalNewLine (),
			[TextEditorCommands.InsertNewLinePreserveCaretPosition] = op => op.OpenLineAbove (),

			[TextEditorCommands.TransposeCharacters] = op => op.TransposeCharacter (),
			[TextEditorCommands.DuplicateLine] = op => op.DuplicateSelection (),

			[TextEditorCommands.DeleteLine] = op => op.DeleteFullLine (),
			[TextEditorCommands.DeleteToLineStart] = op => op.DeleteToBeginningOfLine (),
			[TextEditorCommands.DeleteToLineEnd] = op => op.DeleteToEndOfLine (),
			[TextEditorCommands.DeletePrevWord] = op => op.DeleteWordToLeft (),
			[TextEditorCommands.DeleteNextWord] = op => op.DeleteWordToRight (),

			[TextEditorCommands.MovePrevWord] = op => op.MoveToPreviousWord (extendSelection: false),
			[TextEditorCommands.MoveNextWord] = op => op.MoveToNextWord (extendSelection: false),

			[TextEditorCommands.ExpandSelection] = op => op.SelectEnclosing (),
			[TextEditorCommands.ExpandSelectionToLine] = op => op.MoveToEndOfLine (extendSelection: true),

			[TextEditorCommands.SelectionMoveLeft] = op => op.MoveToPreviousCharacter (extendSelection: true),
			[TextEditorCommands.SelectionMoveRight] = op => op.MoveToNextCharacter (extendSelection: true),
			[TextEditorCommands.SelectionMovePrevWord] = op => op.MoveToPreviousWord (extendSelection: true),
			[TextEditorCommands.SelectionMoveNextWord] = op => op.MoveToNextWord (extendSelection: true),
			[TextEditorCommands.SelectionMoveUp] = op => op.MoveLineUp (extendSelection: true),
			[TextEditorCommands.SelectionMoveDown] = op => op.MoveLineDown (extendSelection: true),
			[TextEditorCommands.SelectionMoveHome] = op => op.MoveToHome (extendSelection: true),
			[TextEditorCommands.SelectionMoveEnd] = op => op.MoveToEndOfLine (extendSelection: true),
			[TextEditorCommands.SelectionMoveToDocumentStart] = op => op.MoveToStartOfDocument (extendSelection: true),
			[TextEditorCommands.SelectionMoveToDocumentEnd] = op => op.MoveToEndOfDocument (extendSelection: true),
			[TextEditorCommands.SelectionPageUpAction] = op => op.PageUp (extendSelection: true),
			[TextEditorCommands.SelectionPageDownAction] = op => op.PageDown (extendSelection: true),

			[TextEditorCommands.RecenterEditor] = op => op.ScrollLineCenter (),

			[EditCommands.InsertGuid] = op => op.InsertText (Guid.NewGuid ().ToString ()),
			[EditCommands.IndentSelection] = op => op.IncreaseLineIndent (),
			[EditCommands.UnIndentSelection] = op => op.DecreaseLineIndent (),
			[EditCommands.UppercaseSelection] = op => op.MakeUppercase (),
			[EditCommands.LowercaseSelection] = op => op.MakeLowercase (),
			[EditCommands.RemoveTrailingWhiteSpaces] = op => op.TrimTrailingWhiteSpace (),

			[ViewCommands.CenterAndFocusCurrentDocument] = op => op.ScrollLineCenter ()
		};

		protected virtual void InstallAdditionalEditorOperationsCommands ()
		{
			EditorOperationCommands[TextEditorCommands.SwitchCaretMode] = op => {
				var overWriteMode = EditorOptions.GetOptionValue (DefaultTextViewOptions.OverwriteModeId);
				EditorOptions.SetOptionValue (DefaultTextViewOptions.OverwriteModeId, !overWriteMode);
			};
		}

		#endregion

		#region Command Mapping Handlers

		// FIXME: this is a hack to explicitly support a few commands when the find/replace view
		// has focus. Longer term we need to improve the MD<->VS commanding to handle this without
		// explicit context checks. Fixes https://devdiv.visualstudio.com/DevDiv/_workitems/edit/821862
		static readonly HashSet<object> commandsSupportedWhenFindPresenterIsFocused = new HashSet<object> {
			CommandManager.ToCommandId (SearchCommands.Find),
			CommandManager.ToCommandId (SearchCommands.Replace),
			CommandManager.ToCommandId (SearchCommands.FindNext),
			CommandManager.ToCommandId (SearchCommands.FindPrevious),
			CommandManager.ToCommandId (SearchCommands.FindNextSelection),
			CommandManager.ToCommandId (SearchCommands.FindPrevious)
		};

		bool CanHandleCommand (object commandId)
		{
			var findPresenter = Imports.FindPresenterFactory?.TryGetFindPresenter (TextView);
			if (findPresenter != null && findPresenter.IsFocused)
				return commandsSupportedWhenFindPresenterIsFocused.Contains (commandId);

			return true;
		}

		ICommandHandler ICustomCommandTarget.GetCommandHandler (object commandId)
		{
			if (!CanHandleCommand (commandId))
				return null;

			if (CommandMappings.Instance.HasMapping (commandId) || EditorOperationCommands.ContainsKey (commandId))
				return this;

			return null;
		}

		ICommandUpdater ICustomCommandTarget.GetCommandUpdater (object commandId)
		{
			if (!CanHandleCommand (commandId))
				return null;

			if (CommandMappings.Instance.HasMapping (commandId) ||
				(EditorOperationCommands.TryGetValue (commandId, out var editorOperationCommand) &&
				editorOperationCommand.Update != null))
				return this;

			return null;
		}

		void ICommandHandler.Run (object cmdTarget, Command cmd)
		{
			var mapping = CommandMappings.Instance.GetMapping (cmd.Id);
			if (mapping != null)
				mapping.Execute (commandService, null);
			else if (EditorOperationCommands.TryGetValue (cmd.Id, out var editorOperationCommand) &&
				editorOperationCommand.Execute != null)
				editorOperationCommand.Execute (EditorOperations);
		}

		void ICommandHandler.Run (object cmdTarget, Command cmd, object dataItem)
			=> throw new InvalidOperationException ("Array commands cannot be mapped to editor commands");

		void ICommandUpdater.Run (object cmdTarget, CommandInfo info)
		{
			var mapping = CommandMappings.Instance.GetMapping (info.Command.Id);
			if (mapping != null) {
				var commandState = mapping.GetCommandState (commandService, null);
				info.Enabled = commandState.IsAvailable;
				info.Visible = !commandState.IsUnspecified;
				info.Checked = commandState.IsChecked;
			} else if (EditorOperationCommands.TryGetValue (info.Command.Id, out var editorOperationCommand) &&
				editorOperationCommand.Update != null)
				editorOperationCommand.Update (EditorOperations, info);
		}

		void ICommandUpdater.Run (object cmdTarget, CommandArrayInfo info)
			=> throw new InvalidOperationException ("Array commands cannot be mapped to editor commands");

		#endregion

		#region IZoomable


#if !WINDOWS
		public bool EnableZoomIn => EditorOperations.CanZoomIn;
		public bool EnableZoomOut => EditorOperations.CanZoomOut;
		public bool EnableZoomReset => EditorOperations.CanZoomReset;

		public void ZoomIn () => EditorOperations.ZoomIn ();
		public void ZoomOut () => EditorOperations.ZoomOut ();
		public void ZoomReset () => EditorOperations.ZoomReset ();
#endif

		#endregion
	}
}

// Missing EditCommands:
//   JoinWithNextLine,
//   MonodevelopPreferences,
//   DefaultPolicies,
//   InsertStandardHeader,
//   EnableDisableFolding,
//   ToggleFolding,
//   ToggleAllFoldings,
//   FoldDefinitions,
//   SortSelectedLines

// Missing RefactoryCommands:
//   CurrentRefactoryOperations
//   FindReferences
//   FindAllReferences
//   FindDerivedClasses
//   DeclareLocal
//   ImportSymbol
//   QuickFix
//   QuickFixMenu

// Missing TextEditorCommands:
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
